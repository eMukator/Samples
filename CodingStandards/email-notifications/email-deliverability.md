# Email — Doručitelnost & Unsubscribe

## Unsubscribe mechanismus

```csharp
// UnsubscribeTokenService.cs
public class UnsubscribeTokenService(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector =
        provider.CreateProtector("Email.Unsubscribe.v1");

    public string GenerateToken(string userId, string emailType)
    {
        var payload = JsonSerializer.Serialize(new
        {
            UserId    = userId,
            Type      = emailType,
            ExpiresAt = DateTime.UtcNow.AddDays(365)
        });
        return Uri.EscapeDataString(_protector.Protect(payload));
    }

    public (string UserId, string EmailType)? ValidateToken(string token)
    {
        try
        {
            var json    = _protector.Unprotect(Uri.UnescapeDataString(token));
            var payload = JsonSerializer.Deserialize<UnsubscribePayload>(json)!;

            if (payload.ExpiresAt < DateTime.UtcNow)
                return null;

            return (payload.UserId, payload.Type);
        }
        catch { return null; }
    }
}

// UnsubscribeController.cs
[AllowAnonymous]
[Route("unsubscribe")]
public class UnsubscribeController(
    UnsubscribeTokenService tokenService,
    IUserPreferencesService preferences) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string token)
    {
        var result = tokenService.ValidateToken(token);
        if (result is null) return View("InvalidToken");

        var (userId, emailType) = result.Value;
        return View(new UnsubscribeViewModel(userId, emailType, token));
    }

    [HttpPost]
    public async Task<IActionResult> Confirm(string token)
    {
        var result = tokenService.ValidateToken(token);
        if (result is null) return View("InvalidToken");

        var (userId, emailType) = result.Value;
        await preferences.UnsubscribeAsync(userId, emailType);

        return View("Success");
    }
}
```

```csharp
// Přidej do každého marketingového emailu
var unsubToken = _tokenService.GenerateToken(userId, "marketing");
var model = new NewsletterModel
{
    // ...
    UnsubscribeUrl  = $"{_baseUrl}/unsubscribe?token={unsubToken}",
    PreferencesUrl  = $"{_baseUrl}/account/email-preferences",
};

// List-Unsubscribe headers (Gmail/Outlook zobrazí jako tlačítko)
var message = new EmailMessage
{
    // ...
    Headers = new()
    {
        ["List-Unsubscribe"]      = $"<{model.UnsubscribeUrl}>",
        ["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click",
        ["List-ID"]               = "<newsletter.example.com>",
        ["Precedence"]            = "bulk",
    }
};
```

---

## Email pravidla pro doručitelnost

```csharp
// ✓ Povinné pro každý email
// 1. Message-ID — unikátní identifikátor pro sledování
message.Headers["Message-ID"] = $"<{Guid.NewGuid():N}@example.com>";

// 2. Plain-text vždy
// HTML + text/plain multipart/alternative

// 3. Preheader text (preview v emailovém klientovi)
// První text v <body> před visible contentem
// <span style="display:none;max-height:0;overflow:hidden;">
//     Preview text 90 znaků...
// </span>

// 4. Inline CSS — emailoví klienti ignorují <style> tagy
// Použij CSS inliner: PreMailer.Net nebo podobný
```

```bash
dotnet add package PreMailer.Net
```

```csharp
// Inline CSS před odesláním
var result  = PreMailer.Net.PreMailer.MoveCssInline(htmlBody);
var inlined = result.Html;
```

---

## DNS záznamy pro doručitelnost

```dns
# SPF — povolené odesílající servery pro doménu
TXT @ "v=spf1 include:sendgrid.net include:mailgun.org ~all"
# ~all = softfail (doporučeno) nebo -all = hardfail

# DKIM — kryptografický podpis emailů (generuje email provider)
# SendGrid:
TXT em1234._domainkey "v=DKIM1; k=rsa; p=MIGfMA0GCSqGSIb3DQEBA..."
# Mailgun:
TXT mailo._domainkey "v=DKIM1; k=rsa; p=..."

# DMARC — politika při selhání SPF/DKIM
TXT _dmarc "v=DMARC1; p=quarantine; pct=100; rua=mailto:dmarc@example.com; ruf=mailto:dmarc@example.com; fo=1"
# p=none → p=quarantine → p=reject (postupně zpřísňuj)

# BIMI — zobrazení loga v emailu (volitelné)
TXT default._bimi "v=BIMI1; l=https://example.com/logo.svg; a=;"
```

### Postup nasazení DMARC

```
1. p=none    — sbírej reporty, nic neblokuj (2 týdny)
2. p=quarantine; pct=10  — 10 % do spam, zbytek normálně
3. p=quarantine; pct=100 — vše do spam pokud selhání
4. p=reject; pct=100     — odmítni podvodné emaily
```

---

## Testování emailů

```csharp
// Vývoj — MailHog (viz docker-compose.yml)
// http://localhost:8025 — webové rozhraní pro zachycené emaily

// appsettings.Development.json
{
  "Email": {
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "SmtpUser": "",
    "SmtpPassword": "",
    "UseSsl": false
  }
}
```

```bash
# Ověření SPF/DKIM/DMARC
# 1. Pošli testovací email na check-auth@verifier.port25.com
# 2. Výsledky obdržíš zpět na tvůj email

# Skóre doručitelnosti
# mail-tester.com — cíl: 10/10
# Google Postmaster Tools — sleduj reputaci domény

# MX toolbox
# https://mxtoolbox.com/SuperTool.aspx
# Zkontroluj SPF, DKIM, DMARC, blacklisty
```

---

## Monitorování emailových metrik

```csharp
// Sleduj v DB nebo email provideru:
// - Delivery rate (cíl: >98 %)
// - Open rate (benchmark: 20-25 % B2C, 15-20 % B2B)
// - Bounce rate (cíl: <2 % — hard bounces rovnou odeber ze seznamu)
// - Spam complaint rate (cíl: <0.1 % — Google: <0.08 %)
// - Unsubscribe rate (>0.5 % = problém s obsahem nebo frekvencí)

// Hard bounce = adresa neexistuje → ihned odeber ze seznamu
// Soft bounce = dočasný problém → zkus 3× pak odeber
public async Task HandleBounceWebhookAsync(BounceEvent bounceEvent)
{
    if (bounceEvent.Type == "hard")
    {
        await _userPreferences.MarkEmailInvalidAsync(bounceEvent.Email);
        _logger.LogWarning("Hard bounce for {Email}", bounceEvent.Email);
    }
}
```
