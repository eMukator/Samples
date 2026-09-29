# API — URL Konvence & OpenAPI Dokumentace

## URL struktura — kompletní přehled

```
# Základní pravidla
- Zdroje: podstatná jména, množné číslo, lowercase, kebab-case
- Bez trailing slash
- Verze v prefixu: /api/v1/

# CRUD
GET    /api/v1/orders                    seznam (filtry v query string)
GET    /api/v1/orders/{id}               detail
POST   /api/v1/orders                    vytvoření → 201 + Location
PUT    /api/v1/orders/{id}               kompletní nahrazení → 200 nebo 204
PATCH  /api/v1/orders/{id}               částečná aktualizace → 200 nebo 204
DELETE /api/v1/orders/{id}               smazání → 204

# Zanořené zdroje — max 2 úrovně
GET    /api/v1/orders/{id}/items
POST   /api/v1/orders/{id}/items
DELETE /api/v1/orders/{id}/items/{itemId}

# Filtrování, řazení, paginace — query string
GET /api/v1/orders?status=confirmed&customerId=5
GET /api/v1/orders?sort=createdAt&dir=desc&cursor=eyJpZCI6NDB9&size=20
GET /api/v1/orders?search=novak

# Akce (slovesa OK pro komplexní operace)
POST /api/v1/orders/{id}/cancel
POST /api/v1/orders/{id}/duplicate
POST /api/v1/invoices/{id}/send
POST /api/v1/payments/{id}/refund
```

### Konvence pro query parametry

```
# Boolean filtry
?active=true&deleted=false

# Výčtové hodnoty
?status=pending,confirmed        nebo  ?status[]=pending&status[]=confirmed

# Rozsahy
?createdFrom=2025-01-01&createdTo=2025-12-31
?totalMin=100&totalMax=5000

# Řazení
?sort=createdAt&dir=desc         (jeden sloupec)
?sort=-createdAt,+total          (více sloupců, prefix ± pro směr)

# Projekce — vrať jen vybraná pole
?fields=id,customerName,total
```

### Špatné vzory

```
✗ /api/v1/getOrders              sloveso v URL
✗ /api/v1/order                  jednotné číslo
✗ /api/v1/Orders                 velká písmena
✗ /api/v1/order_items            podtržítka
✗ /api/v1/orders/                trailing slash
✗ /api/v1/orders?customerId&active   chybí hodnota
```

---

## OpenAPI / Swagger — kompletní nastavení

```csharp
// Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "MyApp API",
        Version     = "v1",
        Description = "REST API pro správu objednávek.",
        Contact     = new OpenApiContact
        {
            Name  = "API Tým",
            Email = "api@example.com",
            Url   = new Uri("https://example.com/support")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url  = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // XML komentáře — přidej do csproj:
    // <GenerateDocumentationFile>true</GenerateDocumentationFile>
    // <NoWarn>$(NoWarn);1591</NoWarn>
    var xmlPath = Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xmlPath);

    // JWT Bearer autentizace v Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type        = SecuritySchemeType.Http,
        Scheme      = "bearer",
        BearerFormat = "JWT",
        Description = "Vlož JWT token. Příklad: eyJhbGci..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Skryj interní endpointy
    options.DocumentFilter<HideInternalEndpointsFilter>();
});

// Swagger UI pouze mimo produkci
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.DefaultModelsExpandDepth(-1);  // skryj modely
    });
}
```

### Dokumentace endpointů — XML komentáře

```csharp
/// <summary>
/// Vrátí detail objednávky podle ID.
/// </summary>
/// <param name="id">Unikátní identifikátor objednávky.</param>
/// <param name="ct">Cancellation token.</param>
/// <returns>Detail objednávky.</returns>
/// <response code="200">Objednávka nalezena a vrácena.</response>
/// <response code="401">Uživatel není přihlášen.</response>
/// <response code="403">Uživatel nemá přístup k této objednávce.</response>
/// <response code="404">Objednávka s daným ID neexistuje.</response>
[HttpGet("{id:int}")]
[ProducesResponseType<OrderDto>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct)
{
    var order = await _orderService.GetByIdAsync(id, ct);
    return order is null ? NotFound() : Ok(order);
}

/// <summary>
/// Vytvoří novou objednávku.
/// </summary>
/// <remarks>
/// Příklad požadavku:
///
///     POST /api/v1/orders
///     {
///         "customerName": "Jan Novák",
///         "items": [
///             { "productId": 1, "quantity": 2 }
///         ]
///     }
///
/// </remarks>
[HttpPost]
[ProducesResponseType<OrderDto>(StatusCodes.Status201Created)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<OrderDto>> Create(
    CreateOrderRequest request, CancellationToken ct)
{
    var order = await _orderService.CreateAsync(request, ct);
    return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
}
```

### Skrytí interních endpointů

```csharp
// HideInternalEndpointsFilter.cs
public class HideInternalEndpointsFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        var internalPaths = document.Paths
            .Where(p => p.Key.StartsWith("/internal/") ||
                        p.Key.StartsWith("/health/"))
            .Select(p => p.Key)
            .ToList();

        foreach (var path in internalPaths)
            document.Paths.Remove(path);
    }
}
```
