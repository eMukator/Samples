# Bezpečnost — Rate Limiting

## Nastavení (.NET 7+)

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Globální limit — všechny requesty
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit        = 100,
                Window             = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit         = 0
            }));

    // Přísný limit pro auth endpointy (login, register, forgot-password)
    options.AddPolicy("AuthPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window      = TimeSpan.FromMinutes(15),
            }));

    // API limit pro registrované uživatele
    options.AddPolicy("ApiPolicy", context =>
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: userId ?? context.Connection.RemoteIpAddress?.ToString() ?? "anon",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit    = 1000,
                Window         = TimeSpan.FromHours(1),
                SegmentsPerWindow = 6,
            });
    });
});

app.UseRateLimiter();
```

```csharp
// Použití na controller / action
[EnableRateLimiting("AuthPolicy")]
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request) { }

[EnableRateLimiting("ApiPolicy")]
[ApiController]
public class OrdersController : ControllerBase { }
```

---

# Bezpečnost — XSS ochrana v Razor

## Razor automatické escapování

```cshtml
@* ✓ Razor escapuje automaticky — bezpečné *@
<p>@Model.UserInput</p>
@* Výstup: <p>&lt;script&gt;alert(1)&lt;/script&gt;</p> *@

@* ✗ Html.Raw — pouze pro DŮVĚRYHODNÝ obsah (tvůj CMS, Markdown po sanitizaci) *@
<div>@Html.Raw(Model.HtmlContent)</div>

@* ✓ Html.Raw s sanitizací pro uživatelský obsah *@
@using Ganss.Xss
@{
    var sanitizer = new HtmlSanitizer();
    var safeHtml  = sanitizer.Sanitize(Model.UserHtmlInput);
}
<div>@Html.Raw(safeHtml)</div>
```

## Atributy — pozor na URL

```cshtml
@* ✓ Href je bezpečné pokud používáš @ *@
<a href="@Model.Url">Odkaz</a>

@* ✗ JavaScript: URL — ověř že URL je bezpečná *@
@{
    var safeUrl = Model.Url?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true
        ? Model.Url : "#";
}
<a href="@safeUrl">Odkaz</a>
```

## JavaScript — data z C# do JS

```cshtml
@* ✓ Serializuj přes JSON — Razor escapuje JSON pro JS kontext *@
<script>
    const config = @Html.Raw(Json.Serialize(new {
        userId   = Model.UserId,
        userName = Model.UserName
    }));
</script>

@* ✗ Nikdy přímo do JS stringu *@
<script>
    var name = "@Model.UserName";  // XSS pokud UserName obsahuje "
</script>
```

## NuGet balíček pro sanitizaci

```xml
<PackageReference Include="HtmlSanitizer" Version="*" />
```

```csharp
// Registrace jako singleton (je thread-safe)
builder.Services.AddSingleton<HtmlSanitizer>(sp =>
{
    var sanitizer = new HtmlSanitizer();
    sanitizer.AllowedTags.Add("iframe");  // pokud potřebuješ YouTube embedy
    sanitizer.AllowedAttributes.Add("class");
    return sanitizer;
});
```
