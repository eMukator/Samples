# Bezpečnost — CSRF & CORS

## CSRF — Cross-Site Request Forgery

### Razor Pages / MVC — automatická ochrana

```csharp
// Program.cs — antiforgery je v ASP.NET Core zapnuto automaticky pro Razor
// Explicitně nastav cookie options
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name       = "__Host-af";  // __Host- prefix = extra bezpečnost
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite   = SameSiteMode.Strict;
    options.Cookie.HttpOnly   = true;
    options.HeaderName        = "X-XSRF-TOKEN";  // pro AJAX požadavky
});
```

```cshtml
@* ✓ Formuláře — vždy přidej antiforgery token *@
<form asp-action="Create" method="post">
    @Html.AntiForgeryToken()   @* nebo <input asp-antiforgery="true"> *@
    @* ... *@
</form>

@* ✓ Tag helper — automaticky přidá token *@
<form asp-controller="Orders" asp-action="Create" method="post">
    @* asp-* tag helpers přidávají token automaticky *@
</form>
```

```csharp
// ✓ Controller — ověř token
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Create(CreateOrderRequest request) { }

// ✓ Globálně pro všechny POST/PUT/PATCH/DELETE
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
```

### AJAX požadavky

```javascript
// ✓ Přečti CSRF token z cookie a posílej v headeru
function getCsrfToken() {
  return document.cookie
    .split(';')
    .find(c => c.trim().startsWith('XSRF-TOKEN='))
    ?.split('=')[1];
}

async function apiPost(url, data) {
  return fetch(url, {
    method:  'POST',
    headers: {
      'Content-Type':  'application/json',
      'X-XSRF-TOKEN':  getCsrfToken() ?? '',
    },
    body: JSON.stringify(data),
  });
}
```

### API endpointy — SameSite cookie stačí

```csharp
// Pro čistě API projekty (bez Razor formulářů) — CSRF riziko eliminuje SameSite=Strict
// a JWT v Authorization headeru (ne v cookie)
// Antiforgery token pro API není nutný pokud:
// 1. Autentizace je JWT v Authorization header (ne cookie)
// 2. CORS je správně nastaven (viz níže)

[ApiController]  // ApiController automaticky vypne antiforgery validaci
[Route("api/[controller]")]
public class OrdersController : ControllerBase { }
```

---

## CORS — Cross-Origin Resource Sharing

```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    // ✓ Pojmenovaná policy pro různé scénáře
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(
                "https://app.example.com",
                "https://www.example.com"
            )
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
            .WithHeaders("Content-Type", "Authorization", "X-XSRF-TOKEN")
            .WithExposedHeaders("X-Correlation-ID", "X-RateLimit-Remaining")
            .SetPreflightMaxAge(TimeSpan.FromHours(1)));

    // ✓ Volnější policy pro veřejné API
    options.AddPolicy("PublicApi", policy =>
        policy
            .AllowAnyOrigin()
            .WithMethods("GET")
            .WithHeaders("Content-Type"));

    // ✗ NIKDY v produkci
    // options.AddPolicy("DangerousAll", policy =>
    //     policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().AllowCredentials());
    // AllowAnyOrigin() a AllowCredentials() nelze kombinovat!
});

app.UseCors("Frontend");  // aplikuj globálně

// nebo per-endpoint
[EnableCors("PublicApi")]
[HttpGet]
public IActionResult GetPublicData() { }
```

### Development CORS

```csharp
// appsettings.Development.json
{
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:3000",
      "https://localhost:7001"
    ]
  }
}

// Program.cs — načti origins z konfigurace
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()));
```

### Preflight caching

```csharp
// ✓ Nastav dlouhý preflight cache pro produkci (méně OPTIONS požadavků)
policy.SetPreflightMaxAge(TimeSpan.FromHours(2));  // max Chrome: 2h, Firefox: 24h
```
