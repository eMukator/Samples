# Bezpečnost — Autentizace & Autorizace

## Autorizace — výchozí zamítnutí

```csharp
// Program.cs — vyžaduj autorizaci všude, výjimky explicitně
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ✓ Veřejné stránky označuj explicitně
[AllowAnonymous]
public class HomeController : Controller { }

// ✗ Nikdy nespoléhej na "zapomněl jsem přidat [Authorize]"
```

## Role a Policy

```csharp
// ✓ Policies místo přímých rolí — flexibilnější
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanManageOrders", policy =>
        policy.RequireRole("Admin", "Manager"));

    options.AddPolicy("CanViewReports", policy =>
        policy.RequireClaim("department", "finance", "management"));

    options.AddPolicy("MinAge18", policy =>
        policy.Requirements.Add(new MinAgeRequirement(18)));
});

// ✓ Použití
[Authorize(Policy = "CanManageOrders")]
public class OrdersController : Controller { }

// ✓ Resource-based authorization pro konkrétní záznamy
public async Task<IActionResult> Edit(int id)
{
    var order = await _orderService.GetByIdAsync(id);
    if (!await _authorizationService.AuthorizeAsync(User, order, "EditOrder"))
        return Forbid();
    // ...
}
```

## Hesla — správné hashování

```csharp
// ✓ ASP.NET Core Identity — hashování je správně automaticky
// ✓ Nebo PasswordHasher přímo
var hasher  = new PasswordHasher<User>();
var hash    = hasher.HashPassword(user, plainTextPassword);
var result  = hasher.VerifyHashedPassword(user, hash, inputPassword);

// ✗ Nikdy vlastní hashování, MD5, SHA1, ani nezašifrované ukládání
// ✗ Nikdy porovnávej hesla přímým string.Equals
```

## JWT — pokud používáš

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = config["Jwt:Issuer"],
            ValidAudience            = config["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
            ClockSkew                = TimeSpan.FromMinutes(1),  // ne výchozích 5 min
        };
    });

// ✓ Krátká platnost access tokenu
var token = new JwtSecurityToken(
    expires: DateTime.UtcNow.AddMinutes(15),  // ne hodiny, ne dny
    // ...
);
```

## Cookie bezpečnost

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly  = true;   // nedostupné z JS
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // pouze HTTPS
    options.Cookie.SameSite = SameSiteMode.Strict;  // CSRF ochrana
    options.ExpireTimeSpan  = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});
```
