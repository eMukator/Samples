# Bezpečnost — HTTP Hlavičky

## Kompletní nastavení v Program.cs

```csharp
// Program.cs
app.UseHsts();   // HSTS — pouze HTTPS
app.UseHttpsRedirection();

// Bezpečnostní hlavičky
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;

    // Zabraňuje MIME type sniffingu
    headers["X-Content-Type-Options"] = "nosniff";

    // Zabraňuje clickjackingu — stránka nesmí být v iframe
    headers["X-Frame-Options"] = "DENY";  // nebo "SAMEORIGIN" pokud potřebuješ iframe

    // Vypne legacy XSS filtr (moderní browsery ho nepotřebují, může být zneužit)
    headers["X-XSS-Protection"] = "0";

    // Referrer — neposílej plnou URL na třetí strany
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // Permissions Policy — zakáže nepotřebné browser API
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";

    // Odstraň informace o serveru
    headers.Remove("Server");
    headers.Remove("X-Powered-By");
    headers.Remove("X-AspNet-Version");

    await next();
});
```

## Content Security Policy (CSP)

CSP je nejúčinnější ochrana proti XSS. Nastavuj postupně — začni s `Content-Security-Policy-Report-Only`.

```csharp
// Základní CSP pro Razor Pages / MVC bez inline JS
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "script-src 'self' 'nonce-{NONCE}'; " +  // nonce pro inline skripty
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "form-action 'self'; " +
        "base-uri 'self'; " +
        "upgrade-insecure-requests;";
    await next();
});
```

### CSP s nonce pro inline skripty (doporučeno pro Razor)

```csharp
// CspNonceMiddleware.cs
public class CspNonceMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        context.Items["CspNonce"] = nonce;

        context.Response.Headers["Content-Security-Policy"] =
            $"default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}'; " +
            $"style-src 'self' 'unsafe-inline'; " +
            $"img-src 'self' data: https:; " +
            $"frame-ancestors 'none'; " +
            $"form-action 'self';";

        await next(context);
    }
}

// _Layout.cshtml — přidej nonce na všechny <script> tagy
@{
    var nonce = Context.Items["CspNonce"]?.ToString();
}
<script src="/js/site.js" nonce="@nonce"></script>
<script nonce="@nonce">
    // inline JS
</script>
```

## HSTS konfigurace

```csharp
// Program.cs
builder.Services.AddHsts(options =>
{
    options.Preload           = true;
    options.IncludeSubDomains = true;
    options.MaxAge            = TimeSpan.FromDays(365);
});
```

## Ověření hlaviček

Testuj na: [securityheaders.com](https://securityheaders.com)

Cílová hodnocení:
- SecurityHeaders.com: **A** nebo **A+**
- Mozilla Observatory: **A** nebo **A+**
