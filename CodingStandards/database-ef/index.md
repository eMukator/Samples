# Databáze & EF Core Standards

## Moduly

@db-migrations-indexes.md
@db-ef-patterns.md

## Rychlá kontrola

- [ ] Migrace má popisný PascalCase název
- [ ] Existující migrace po deployi NIKDY neupravovat
- [ ] Foreign key sloupce mají index
- [ ] Soft delete přes ISoftDeletable + global query filter
- [ ] Audit columns (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
- [ ] AsNoTracking() pro read-only dotazy
- [ ] Žádné N+1 dotazy (Include nebo projection)
- [ ] SaveChangesAsync s ošetřením DbUpdateConcurrencyException
