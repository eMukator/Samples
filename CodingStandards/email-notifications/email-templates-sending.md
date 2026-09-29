# Email — Šablony & Odesílání

## Architektura emailového systému

```
Request → Service → OutboxEmail (DB) → BackgroundJob → EmailProvider
                                                        (SMTP/SendGrid/Mailgun)
```

Nikdy neodesílej email synchronně v request pipeline — použij Outbox pattern.

## Interface a modely

```csharp
// IEmailService.cs
public interface IEmailService
{
    Task<bool> SendAsync(EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage
{
    public required string   To          { get; init; }
    public required string   Subject     { get; init; }
    public required string   HtmlBody    { get; init; }
    public required string   TextBody    { get; init; }  // vždy plain-text alternativa
    public string?           From        { get; init; }
    public string?           ReplyTo     { get; init; }
    public string?           IdempotencyKey { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public List<EmailAttachment> Attachments  { get; init; } = [];
}

public record EmailAttachment(
    string   FileName,
    byte[]   Content,
    string   MimeType);
```

## Šablony — Razor šablony

```bash
dotnet add package RazorLight  # renderování Razor mimo HTTP pipeline
```

```csharp
// EmailTemplateService.cs
public class EmailTemplateService
{
    private readonly IRazorLightEngine _engine;

    public EmailTemplateService()
    {
        _engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(
                AppContext.BaseDirectory, "EmailTemplates"))
            .UseMemoryCachingProvider()
            .Build();
    }

    public async Task<(string Html, string Text)> RenderAsync<T>(
        string templateName, T model)
    {
        var html = await _engine.CompileRenderAsync(
            $"{templateName}.cshtml", model);
        var text = await _engine.CompileRenderAsync(
            $"{templateName}.txt", model);  // plain-text varianta
        return (html, text);
    }
}
```

```
EmailTemplates/
  order-confirmation.cshtml
  order-confirmation.txt
  password-reset.cshtml
  password-reset.txt
  welcome.cshtml
  welcome.txt
  _layout.cshtml              ← sdílené hlavičky/patičky
```

```cshtml
@* order-confirmation.cshtml *@
@model OrderConfirmationModel
@{
    Layout = "_layout";
    ViewBag.PreviewText = $"Vaše objednávka #{Model.OrderNumber} byla přijata";
}

<h1 style="color:#1a56db;font-family:Arial,sans-serif;font-size:24px;">
    Potvrzení objednávky #@Model.OrderNumber
</h1>

<p style="font-family:Arial,sans-serif;font-size:16px;line-height:1.5;">
    Dobrý den, @Model.CustomerName,
</p>

<p style="font-family:Arial,sans-serif;">
    Vaše objednávka byla úspěšně přijata a zpracovává se.
</p>

<table style="width:100%;border-collapse:collapse;margin:20px 0;">
    <thead>
        <tr style="background:#f3f4f6;">
            <th style="padding:10px;text-align:left;border:1px solid #e5e7eb;">Produkt</th>
            <th style="padding:10px;text-align:right;border:1px solid #e5e7eb;">Množství</th>
            <th style="padding:10px;text-align:right;border:1px solid #e5e7eb;">Cena</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model.Items)
        {
            <tr>
                <td style="padding:10px;border:1px solid #e5e7eb;">@item.ProductName</td>
                <td style="padding:10px;text-align:right;border:1px solid #e5e7eb;">@item.Quantity</td>
                <td style="padding:10px;text-align:right;border:1px solid #e5e7eb;">@item.LineTotal.ToString("C")</td>
            </tr>
        }
    </tbody>
    <tfoot>
        <tr>
            <td colspan="2" style="padding:10px;text-align:right;font-weight:bold;">Celkem:</td>
            <td style="padding:10px;text-align:right;font-weight:bold;">@Model.Total.ToString("C")</td>
        </tr>
    </tfoot>
</table>

<p style="font-size:12px;color:#6b7280;margin-top:30px;">
    Tento e-mail byl zaslán automaticky. Neodpovídejte na něj.
    <a href="@Model.UnsubscribeUrl" style="color:#6b7280;">Odhlásit ze zasílání</a>
</p>
```

## Outbox Pattern — spolehlivé odesílání

```csharp
// OutboxEmail.cs — entita v DB
public class OutboxEmail
{
    public int             Id           { get; set; }
    public string          To           { get; set; } = default!;
    public string          Subject      { get; set; } = default!;
    public string          HtmlBody     { get; set; } = default!;
    public string          TextBody     { get; set; } = default!;
    public DateTimeOffset  CreatedAt    { get; set; }
    public DateTimeOffset? SentAt       { get; set; }
    public int             RetryCount   { get; set; }
    public string?         LastError    { get; set; }
    public DateTimeOffset? NextRetryAt  { get; set; }
}

// OutboxEmailService.cs — uloží do DB místo přímého odeslání
public class OutboxEmailService(AppDbContext context) : IEmailService
{
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken ct)
    {
        context.OutboxEmails.Add(new OutboxEmail
        {
            To        = message.To,
            Subject   = message.Subject,
            HtmlBody  = message.HtmlBody,
            TextBody  = message.TextBody,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(ct);
        return true;  // uloženo, ještě neodesláno
    }
}

// OutboxEmailJob.cs — Background Service pro odesílání
public class OutboxEmailJob(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxEmailJob> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await ProcessPendingEmailsAsync(ct); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "OutboxEmailJob iteration failed"); }

            await Task.Delay(_interval, ct);
        }
    }

    private async Task ProcessPendingEmailsAsync(CancellationToken ct)
    {
        using var scope   = scopeFactory.CreateScope();
        var context       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var smtpService   = scope.ServiceProvider.GetRequiredService<ISmtpEmailService>();
        var now           = DateTimeOffset.UtcNow;

        var pending = await context.OutboxEmails
            .Where(e => e.SentAt == null
                     && e.RetryCount < 3
                     && (e.NextRetryAt == null || e.NextRetryAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var email in pending)
        {
            try
            {
                await smtpService.SendAsync(new EmailMessage
                {
                    To       = email.To,
                    Subject  = email.Subject,
                    HtmlBody = email.HtmlBody,
                    TextBody = email.TextBody,
                }, ct);

                email.SentAt = now;
                logger.LogInformation("Email sent to {To}: {Subject}", email.To, email.Subject);
            }
            catch (Exception ex)
            {
                email.RetryCount++;
                email.LastError   = ex.Message;
                // Exponenciální backoff: 5min, 30min, 2hod
                email.NextRetryAt = now.AddMinutes(Math.Pow(6, email.RetryCount) * 5);
                logger.LogWarning(ex, "Email send failed (attempt {N}/3) to {To}",
                    email.RetryCount, email.To);
            }
        }

        await context.SaveChangesAsync(ct);
    }
}
```
