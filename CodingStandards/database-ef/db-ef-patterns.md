# Databáze — EF Core Vzory

## Soft Delete

```csharp
// ISoftDeletable.cs
public interface ISoftDeletable
{
    DateTimeOffset? DeletedAt { get; set; }
    string?         DeletedBy { get; set; }
}

// AppDbContext.cs — global query filter automaticky filtruje smazané záznamy
protected override void OnModelCreating(ModelBuilder mb)
{
    foreach (var entityType in mb.Model.GetEntityTypes())
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)) continue;

        // Přidej global filter pro každou soft-deletable entitu
        var parameter = Expression.Parameter(entityType.ClrType, "e");
        var property  = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var isNull    = Expression.Equal(property, Expression.Constant(null, typeof(DateTimeOffset?)));
        var lambda    = Expression.Lambda(isNull, parameter);

        mb.Entity(entityType.ClrType).HasQueryFilter(lambda);
    }
}

// Přepiš SaveChangesAsync — zachyť Delete a nahraď Soft Delete
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    foreach (var entry in ChangeTracker.Entries<ISoftDeletable>()
        .Where(e => e.State == EntityState.Deleted))
    {
        entry.State            = EntityState.Modified;
        entry.Entity.DeletedAt = _clock.UtcNow;
        entry.Entity.DeletedBy = _currentUser.Id;
    }

    return await base.SaveChangesAsync(ct);
}

// Použití — normální delete
await _context.Orders.Where(o => o.Id == id).ExecuteDeleteAsync(ct);
// → Ve skutečnosti nastaví DeletedAt, fyzicky nesmaže

// Obejití filtru (admin, audit)
var allIncludingDeleted = await _context.Orders
    .IgnoreQueryFilters()
    .ToListAsync(ct);
```

---

## Audit Columns — automatické nastavení

```csharp
// AuditableEntity.cs
public abstract class AuditableEntity : ISoftDeletable
{
    public int             Id        { get; set; }
    public DateTimeOffset  CreatedAt { get; set; }
    public string          CreatedBy { get; set; } = default!;
    public DateTimeOffset? UpdatedAt { get; set; }
    public string?         UpdatedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string?         DeletedBy { get; set; }
}

// AppDbContext.cs
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var now    = _clock.UtcNow;
    var userId = _currentUser.Id ?? "system";

    foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = userId;
                break;

            case EntityState.Modified:
                // Zabraň přepsání CreatedAt / CreatedBy
                entry.Property(e => e.CreatedAt).IsModified = false;
                entry.Property(e => e.CreatedBy).IsModified = false;
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = userId;
                break;
        }
    }

    return await base.SaveChangesAsync(ct);
}

// ICurrentUser.cs — získání přihlášeného uživatele v DbContext
public interface ICurrentUser
{
    string? Id   { get; }
    string? Name { get; }
}

public class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public string? Id   => accessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    public string? Name => accessor.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;
}
```

---

## N+1 Prevence

```csharp
// ✗ N+1 — jeden SELECT pro seznam + N SELECT pro každý detail
var orders = await _context.Orders.ToListAsync(ct);
foreach (var order in orders)
{
    // Každý přístup na navigaci = nový SQL dotaz!
    var customerName = order.Customer.Name;
    var itemCount    = order.Items.Count;
}

// ✓ Eager loading s Include
var orders = await _context.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .ThenInclude(i => i.Product)
    .ToListAsync(ct);

// ✓ Projection — nejefektivnější, načti jen co potřebuješ
var dtos = await _context.Orders
    .Select(o => new OrderListDto(
        o.Id,
        o.Customer.Name,          // SQL JOIN automaticky
        o.Items.Count,            // SQL COUNT automaticky
        o.Items.Sum(i => i.Price * i.Quantity),
        o.Status,
        o.CreatedAt))
    .ToListAsync(ct);

// ✓ Split query pro velké kolekce (zabraňuje kartézskému součinu)
// Místo jednoho JOIN který vytvoří M×N řádků
var orders = await _context.Orders
    .Include(o => o.Items)
    .AsSplitQuery()   // 2 samostatné SQL dotazy místo jednoho obrovského JOINu
    .ToListAsync(ct);
```

### Detekce N+1 — EF Core Warning

```csharp
// Program.cs — warningy pro lazy loading a N+1
options.UseSqlServer(connectionString, sqlOptions => { })
    .ConfigureWarnings(warnings =>
    {
        warnings.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
        warnings.Log(CoreEventId.LazyLoadOnDisposedContextWarning);
    });
```

---

## Optimistická Concurrency

```csharp
// Entita s row version
public class Order : AuditableEntity
{
    // SQL Server: rowversion / timestamp
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    // Nebo pro jiné DB: Guid concurrency token
    // public Guid ConcurrencyToken { get; set; } = Guid.NewGuid();
}

// Konfigurace
builder.Property(o => o.RowVersion).IsRowVersion();

// Zachycení konfliktu
try
{
    await _context.SaveChangesAsync(ct);
}
catch (DbUpdateConcurrencyException ex)
{
    var entry    = ex.Entries.Single();
    var dbValues = await entry.GetDatabaseValuesAsync(ct);

    if (dbValues is null)
        throw new NotFoundException($"Záznam byl mezitím smazán.");

    // Loguj a vrať 409 Conflict
    _logger.LogWarning("Concurrency conflict on {Entity} {Id}",
        entry.Entity.GetType().Name,
        ((AuditableEntity)entry.Entity).Id);

    throw new ConflictException("Záznam byl mezitím upraven jiným uživatelem. Obnovte stránku.");
}
```

---

## Query Optimization — obecné vzory

```csharp
// ✓ AsNoTracking pro read-only dotazy (méně paměti, rychlejší)
var orders = await _context.Orders
    .AsNoTracking()
    .Where(o => o.Status == OrderStatus.Confirmed)
    .ToListAsync(ct);

// ✓ ExecuteUpdate / ExecuteDelete pro hromadné operace (bez načítání entit)
await _context.Orders
    .Where(o => o.Status == OrderStatus.Pending
             && o.CreatedAt < DateTimeOffset.UtcNow.AddDays(-30))
    .ExecuteUpdateAsync(s =>
        s.SetProperty(o => o.Status, OrderStatus.Expired)
         .SetProperty(o => o.UpdatedAt, DateTimeOffset.UtcNow),
        ct);

// ✓ Compiled queries pro opakované dotazy s různými parametry
private static readonly Func<AppDbContext, int, Task<Order?>> GetOrderById =
    EF.CompileAsyncQuery((AppDbContext ctx, int id) =>
        ctx.Orders
            .Include(o => o.Items)
            .FirstOrDefault(o => o.Id == id));

// Použití
var order = await GetOrderById(_context, id);
```
