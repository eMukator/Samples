# Bezpečnost — Injection (SQL, Command, Path)

## SQL Injection — EF Core

```csharp
// ✗ KRITICKÁ ZRANITELNOST — nikdy nepoužívej řetězení
var orders = _context.Orders
    .FromSqlRaw($"SELECT * FROM Orders WHERE Name = '{name}'");

// ✗ Stejně špatné — string interpolace
var sql = "SELECT * FROM Orders WHERE Name = '" + name + "'";

// ✓ EF Core LINQ — parametry jsou automaticky escapovány
var orders = await _context.Orders
    .Where(o => o.CustomerName == name)
    .ToListAsync(ct);

// ✓ Raw SQL nutný — použij FromSqlInterpolated (escapuje automaticky)
var orders = await _context.Orders
    .FromSqlInterpolated($"SELECT * FROM Orders WHERE Name = {name}")
    .ToListAsync(ct);

// ✓ Raw SQL s explicitními parametry
var param = new SqlParameter("@name", name);
var orders = await _context.Orders
    .FromSqlRaw("SELECT * FROM Orders WHERE Name = @name", param)
    .ToListAsync(ct);

// ✓ Dapper — vždy anonymní objekt pro parametry
var orders = await connection.QueryAsync<Order>(
    "SELECT * FROM Orders WHERE Name = @Name AND Status = @Status",
    new { Name = name, Status = status });
```

## OS Command Injection

```csharp
// ✗ Nikdy nespouštěj shell příkazy se vstupem od uživatele
Process.Start("cmd.exe", $"/c convert {userFilename}");

// ✓ Validuj a sanitizuj název souboru
var safeFileName = Path.GetFileName(userInput);  // odstraní path traversal
if (!Regex.IsMatch(safeFileName, @"^[\w\-. ]+$"))
    throw new ArgumentException("Neplatný název souboru");

var fullPath = Path.Combine(_uploadPath, safeFileName);
// Ověř, že výsledná cesta je stále v povoleném adresáři
if (!fullPath.StartsWith(_uploadPath, StringComparison.OrdinalIgnoreCase))
    throw new SecurityException("Path traversal detekován");
```

## Path Traversal

```csharp
// ✗ Zranitelné — útočník může zadat ../../etc/passwd
var path = Path.Combine(uploadsDir, userProvidedFilename);
return File(path, "application/octet-stream");

// ✓ Bezpečné — ověř, že cesta je uvnitř povoleného adresáře
public IActionResult DownloadFile(string filename)
{
    // Normalizuj cestu a ověř prefix
    var safeName    = Path.GetFileName(filename);  // odstraní adresáře
    var fullPath    = Path.GetFullPath(Path.Combine(_uploadsDir, safeName));
    var allowedBase = Path.GetFullPath(_uploadsDir);

    if (!fullPath.StartsWith(allowedBase + Path.DirectorySeparatorChar))
        return BadRequest("Neplatná cesta");

    if (!System.IO.File.Exists(fullPath))
        return NotFound();

    return PhysicalFile(fullPath, "application/octet-stream", safeName);
}
```

## Mass Assignment

```csharp
// ✗ Bind celý model — útočník může nastavit IsAdmin = true
[HttpPost]
public async Task<IActionResult> Update([FromBody] User user)
{
    _context.Update(user);  // přepíše VŠE včetně Role, IsAdmin...
}

// ✓ Bind pouze povolená pole přes DTO
public record UpdateProfileRequest(string FirstName, string LastName, string? Bio);

[HttpPost]
public async Task<IActionResult> Update([FromBody] UpdateProfileRequest request)
{
    var user = await _context.Users.FindAsync(UserId);
    user!.FirstName = request.FirstName;
    user.LastName   = request.LastName;
    user.Bio        = request.Bio;
    await _context.SaveChangesAsync(ct);
}

// ✓ Nebo [Bind] atribut
[HttpPost]
public async Task<IActionResult> Update(
    [Bind("FirstName,LastName,Bio")] User user) { }
```

## XML External Entity (XXE)

```csharp
// ✓ Bezpečné nastavení XML parseru
var settings = new XmlReaderSettings
{
    DtdProcessing    = DtdProcessing.Prohibit,  // zakáže DTD
    XmlResolver      = null,                     // žádné externí entity
    MaxCharactersFromEntities = 1024
};

using var reader = XmlReader.Create(inputStream, settings);
```
