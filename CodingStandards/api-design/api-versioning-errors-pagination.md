# API — Verzování, Error Responses & Paginace

## Verzování API

```csharp
// Program.cs
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion                   = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions                   = true;  // Api-Supported-Versions header
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat           = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});
```

```csharp
// Controller s verzováním
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : ControllerBase
{
    // Dostupné v obou verzích
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderDto>> GetById(int id, CancellationToken ct) { }

    // Pouze v v1
    [HttpGet, MapToApiVersion("1.0")]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetV1(
        [FromQuery] OffsetPageRequest request, CancellationToken ct) { }

    // Pouze v v2 — nová cursor-based paginace
    [HttpGet, MapToApiVersion("2.0")]
    public async Task<ActionResult<PagedResponse<OrderDto>>> GetV2(
        [FromQuery] CursorPageRequest request, CancellationToken ct) { }
}
```

### Strategie verzování

```
URL prefix (doporučeno):     /api/v1/orders
Query parameter:             /api/orders?api-version=1.0
Header:                      Api-Version: 1.0
Media type:                  Accept: application/vnd.myapi.v1+json
```

### Deprecation

```csharp
[ApiVersion("1.0", Deprecated = true)]
// Přidá header: api-deprecated-versions: 1.0
// Přidá header: api-supported-versions: 2.0
```

---

## Error Response — RFC 9457 Problem Details

```json
// Standardní chybová odpověď
{
  "type":     "https://example.com/problems/validation-error",
  "title":    "Validační chyba",
  "status":   400,
  "detail":   "Jeden nebo více polí obsahuje neplatné hodnoty.",
  "instance": "/api/v1/orders",
  "traceId":  "00-abc123def456-78901234-00",
  "errors": {
    "customerName": ["Pole je povinné.", "Maximální délka je 100 znaků."],
    "items":        ["Objednávka musí mít alespoň jednu položku."]
  }
}
```

```csharp
// GlobalExceptionHandler.cs
public class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, type, title) = exception switch
        {
            ValidationException ex      => (400, "validation-error",     ex.Message),
            NotFoundException ex        => (404, "not-found",             ex.Message),
            ConflictException ex        => (409, "conflict",              ex.Message),
            DomainException ex          => (422, "domain-rule-violation", ex.Message),   // ddd/ddd-tactical.md
            RateLimitException          => (429, "rate-limit-exceeded",   "Příliš mnoho požadavků."),
            UnauthorizedAccessException => (403, "forbidden",             "Přístup odepřen."),
            _                           => (500, "internal-server-error", "Interní chyba serveru.")
        };

        if (status >= 500)
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                context.Request.Method, context.Request.Path);

        context.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new()
        {
            HttpContext    = context,
            Exception      = exception,
            ProblemDetails =
            {
                Type     = $"https://example.com/problems/{type}",
                Title    = title,
                Status   = status,
                Instance = context.Request.Path,
            }
        });
    }
}

// Program.cs
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
app.UseExceptionHandler();
```

---

## Paginace — cursor-based

```csharp
// Request
public record CursorPageRequest(
    int     Size    = 20,
    string? Cursor  = null,
    string  SortBy  = "id",
    string  SortDir = "desc")
{
    public int Size { get; init; } = Math.Clamp(Size, 1, 100);
}

// Response
public record PagedResponse<T>(
    IReadOnlyList<T> Items,
    string?          NextCursor,
    bool             HasMore,
    int?             TotalCount = null);   // volitelné — nákladné pro velká data

// Implementace
public async Task<PagedResponse<OrderDto>> GetPagedAsync(
    CursorPageRequest request, CancellationToken ct)
{
    IQueryable<Order> query = _context.Orders.AsNoTracking();

    // Cursor dekódování
    if (request.Cursor is not null)
    {
        var lastId = DecodeCursor(request.Cursor);
        query = request.SortDir == "desc"
            ? query.Where(o => o.Id < lastId)
            : query.Where(o => o.Id > lastId);
    }

    // Řazení
    query = (request.SortBy, request.SortDir) switch
    {
        ("id",        "desc") => query.OrderByDescending(o => o.Id),
        ("id",        _)      => query.OrderBy(o => o.Id),
        ("createdAt", "desc") => query.OrderByDescending(o => o.CreatedAt),
        ("createdAt", _)      => query.OrderBy(o => o.CreatedAt),
        _                     => query.OrderByDescending(o => o.Id)
    };

    // Načti +1 pro detekci HasMore
    var items = await query
        .Take(request.Size + 1)
        .Select(o => new OrderDto(o.Id, o.CustomerName, o.Total, o.Status))
        .ToListAsync(ct);

    var hasMore = items.Count > request.Size;
    if (hasMore) items.RemoveAt(items.Count - 1);

    var nextCursor = hasMore ? EncodeCursor(items.Last().Id) : null;
    return new PagedResponse<OrderDto>(items, nextCursor, hasMore);
}

private static string EncodeCursor(int id)
    => Convert.ToBase64String(Encoding.UTF8.GetBytes(id.ToString()));

private static int DecodeCursor(string cursor)
    => int.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(cursor)));
```

---

## Response Headers — metadata

```csharp
// ✓ Vždy přidej Location po 201 Created
[HttpPost]
public async Task<ActionResult<OrderDto>> Create(
    CreateOrderRequest request, CancellationToken ct)
{
    var order = await _orderService.CreateAsync(request, ct);
    return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    // Automaticky přidá: Location: /api/v1/orders/42
}

// ✓ Rate limit hlavičky
Response.Headers["X-RateLimit-Limit"]     = "1000";
Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
Response.Headers["X-RateLimit-Reset"]     = resetTime.ToUnixTimeSeconds().ToString();

// ✓ Retry-After pro 429 a 503
Response.Headers["Retry-After"] = "60";  // vteřiny
```
