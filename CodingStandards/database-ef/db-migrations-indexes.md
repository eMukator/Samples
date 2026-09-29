# Databáze — Migrace & Indexy

## Migrace — konvence a pravidla

### Pojmenování

```bash
# Formát: PascalCase, popisné, začíná slovesem nebo názvem entity
dotnet ef migrations add CreateOrdersTable
dotnet ef migrations add AddCustomerEmailIndex
dotnet ef migrations add AddOrderStatusColumn
dotnet ef migrations add RenameOrderTotalToSubtotal
dotnet ef migrations add AddCustomerEmailUniqueConstraint
dotnet ef migrations add SeedInitialProductCategories
dotnet ef migrations add DropLegacyAuditTable

# ✗ Špatné názvy
dotnet ef migrations add Migration1
dotnet ef migrations add Fix
dotnet ef migrations add Update
dotnet ef migrations add Changes20250101
```

### Pravidla

```csharp
// ✓ Nikdy neupravuj existující migraci po deployi na produkci
// ✓ Chybu oprav novou migrací

// ✓ Migrace musí být zpětně kompatibilní kde je to možné
// (blue-green deployment — nová verze kódu + stará DB musí fungovat)

// ✓ Destruktivní změny rozdel do více kroků:
// Krok 1: Přidej nový sloupec (nullable)
// Krok 2: Deploy kódu který zapisuje do obou sloupců
// Krok 3: Migruj data
// Krok 4: Přidej NOT NULL constraint
// Krok 5: Smaž starý sloupec

// ✓ Data migrace patří do samostatné migrace
protected override void Up(MigrationBuilder mb)
{
    // 1. Schéma
    mb.AddColumn<string>("Region", "Customers", nullable: true);

    // 2. Data migrace — přímo v SQL pro výkon
    mb.Sql(@"
        UPDATE Customers
        SET Region = CASE
            WHEN PostalCode LIKE '1%' THEN 'Praha'
            WHEN PostalCode LIKE '6%' THEN 'Brno'
            ELSE 'Ostatní'
        END");

    // 3. Constraint po migraci dat
    mb.AlterColumn<string>("Region", "Customers", nullable: false);
}
```

### Generování s parametry pro EF Core

```bash
# Startup projekt je nutný pokud máš více projektů
dotnet ef migrations add AddOrderIndex \
    --project src/MyApp.Infrastructure \
    --startup-project src/MyApp.Web \
    --context AppDbContext

# Vygeneruj SQL skript pro produkci (bez automatické migrace)
dotnet ef migrations script \
    --from 20250101_InitialCreate \
    --to 20250215_AddOrderIndex \
    --output migrations.sql \
    --idempotent   # přidá IF NOT EXISTS kontroly
```

### Aplikace migrací — produkce

```csharp
// ✓ Explicitní migrace při startu (pro malé aplikace)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (app.Environment.IsProduction())
    {
        // Loguj pending migrace
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migrations: {Names}",
                pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync();
        }
    }
}
```

---

## Indexy — kdy a jak

### Automatické indexy (EF Core přidá sám)

```
- Primary key → clustered index automaticky
- Foreign key → EF Core přidá index automaticky od verze 7
- Unique constraint → automaticky
```

### Manuální indexy — konfigurace

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // ✓ Sloupce používané ve WHERE klauzulích
        builder.HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");

        // ✓ Sloupce pro řazení (ORDER BY) v kombinaci s WHERE
        builder.HasIndex(o => o.CreatedAt)
            .IsDescending()
            .HasDatabaseName("IX_Orders_CreatedAt_DESC");

        // ✓ Composite index pro časté kombinace filtrů
        builder.HasIndex(o => new { o.CustomerId, o.Status })
            .HasDatabaseName("IX_Orders_CustomerId_Status");

        // ✓ Covering index — zahrnuje často dotazované sloupce
        builder.HasIndex(o => o.Status)
            .IncludeProperties(o => new { o.Total, o.CreatedAt })
            .HasDatabaseName("IX_Orders_Status_Covering");

        // ✓ Filtered index — pro soft delete (jen aktivní záznamy)
        builder.HasIndex(o => o.CustomerId)
            .HasFilter("[DeletedAt] IS NULL")
            .HasDatabaseName("IX_Orders_CustomerId_Active");

        // ✓ Unique s podmínkou (SQL Server)
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("UIX_Orders_OrderNumber");
    }
}
```

### Kdy přidat index — rozhodovací strom

```
1. Je sloupec ve WHERE klauzuli? → Zvažuj index
2. Je sloupec v ORDER BY / GROUP BY? → Zvažuj index (zvláště s WHERE)
3. Je to foreign key? → EF Core přidá automaticky
4. Jak selektivní je sloupec?
   - Status (5 hodnot) → nízká selektivita → index pomůže jen s covering
   - Email (unikátní) → vysoká selektivita → index velmi pomůže
5. Kolik zápisů vs. čtení? Indexy zpomalují INSERT/UPDATE
   - Read-heavy (reporty): více indexů
   - Write-heavy (logování): méně indexů
```

### Sledování nevyužitých indexů

```sql
-- SQL Server — nevyužité indexy (spusť po týdnu produkčního provozu)
SELECT
    OBJECT_NAME(i.object_id) AS TableName,
    i.name                   AS IndexName,
    s.user_seeks,
    s.user_scans,
    s.user_lookups,
    s.user_updates
FROM sys.indexes i
LEFT JOIN sys.dm_db_index_usage_stats s
    ON i.object_id = s.object_id
    AND i.index_id = s.index_id
    AND s.database_id = DB_ID()
WHERE i.type > 0
    AND OBJECT_NAME(i.object_id) NOT LIKE 'sys%'
    AND (s.user_seeks + s.user_scans + s.user_lookups = 0
         OR s.user_seeks IS NULL)
ORDER BY s.user_updates DESC;
```
