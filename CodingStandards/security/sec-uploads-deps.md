# Bezpečnost — Upload souborů, Závislosti & Citlivá data

## Upload souborů — bezpečná implementace

```csharp
// FileUploadService.cs
public class FileUploadService
{
    // ✓ Povolené MIME typy — whitelist, ne blacklist
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    // ✓ Maximální velikost souboru
    private const long MaxFileSizeBytes = 10 * 1024 * 1024;  // 10 MB

    public async Task<string> UploadAsync(IFormFile file, CancellationToken ct)
    {
        // 1. Ověř velikost
        if (file.Length > MaxFileSizeBytes)
            throw new ValidationException($"Soubor nesmí být větší než 10 MB.");

        // 2. Ověř MIME type — NEVĚŘ ContentType z požadavku, ověř magic bytes
        var mimeType = await DetectMimeTypeAsync(file, ct);
        if (!AllowedMimeTypes.Contains(mimeType))
            throw new ValidationException($"Nepodporovaný typ souboru: {mimeType}");

        // 3. Generuj bezpečný název — nikdy nepoužívej původní název
        var safeExtension = GetSafeExtension(mimeType);
        var storedName    = $"{Guid.NewGuid():N}{safeExtension}";

        // 4. Ulož mimo wwwroot — soubory nesmí být přímo přístupné přes URL
        var uploadPath = Path.Combine(_options.UploadBasePath, storedName);
        await using var stream = File.OpenWrite(uploadPath);
        await file.CopyToAsync(stream, ct);

        return storedName;
    }

    // ✓ Detekce MIME typu přes magic bytes (ne ContentType header)
    private static async Task<string> DetectMimeTypeAsync(IFormFile file, CancellationToken ct)
    {
        var buffer = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(buffer, ct);

        return buffer switch
        {
            _ when buffer[..2].SequenceEqual(new byte[] { 0xFF, 0xD8 })             => "image/jpeg",
            _ when buffer[..4].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47 }) => "image/png",
            _ when buffer[..4].SequenceEqual(new byte[] { 0x52, 0x49, 0x46, 0x46 }) => "image/webp",
            _ when buffer[..4].SequenceEqual(new byte[] { 0x25, 0x50, 0x44, 0x46 }) => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    private static string GetSafeExtension(string mimeType) => mimeType switch
    {
        "image/jpeg"       => ".jpg",
        "image/png"        => ".png",
        "image/webp"       => ".webp",
        "image/gif"        => ".gif",
        "application/pdf"  => ".pdf",
        _                  => ".bin"
    };
}
```

```csharp
// Program.cs — limit velikosti requestu
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;  // 10 MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
```

---

## Závislosti — skenování CVE

```bash
# ✓ Kontrola zranitelných balíčků (spouštěj v CI)
dotnet list package --vulnerable --include-transitive

# Výstup:
# Project 'MyApp.Web' has the following vulnerable packages
#    [net9.0]:
#    Top-level Package      Requested   Resolved   Severity   Advisory URL
#    > Newtonsoft.Json       12.0.1      12.0.1     High       https://github.com/...

# ✓ Automatické aktualizace závislostí — Dependabot
```

```yaml
# .github/dependabot.yml
version: 2
updates:
  - package-ecosystem: nuget
    directory: "/"
    schedule:
      interval: weekly
      day: monday
    open-pull-requests-limit: 10
    groups:
      microsoft:
        patterns: ["Microsoft.*", "System.*"]
      testing:
        patterns: ["xunit*", "FluentAssertions", "Moq", "NSubstitute"]
```

```bash
# ✓ Pravidelný audit (přidej do CI pipeline)
dotnet list package --outdated
dotnet list package --vulnerable --include-transitive | grep -v "has no vulnerable"
```

---

## Citlivá data — ochrana v aplikaci

```csharp
// ✓ Data Protection API pro šifrování citlivých dat v DB
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>()  // nebo Azure Key Vault
    .SetApplicationName("MyApp")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Použití
public class PersonalDataService(IDataProtectionProvider provider)
{
    private readonly IDataProtector _protector =
        provider.CreateProtector("PersonalData.v1");

    public string Encrypt(string plainText) => _protector.Protect(plainText);
    public string Decrypt(string cipherText) => _protector.Unprotect(cipherText);
}
```

```csharp
// ✓ Maskovanie citlivých dat v logech
public class Order
{
    public string CustomerEmail { get; set; } = default!;

    // ToString() nikdy neobsahuje citlivé údaje
    public override string ToString()
        => $"Order #{Id} ({Status}) — email: {MaskEmail(CustomerEmail)}";

    private static string MaskEmail(string email)
    {
        var at  = email.IndexOf('@');
        if (at <= 1) return "***@***";
        return $"{email[0]}***{email[(at - 1)..]}";  // j***@example.com
    }
}

// ✓ [PersonalData] atribut pro GDPR — ASP.NET Core Identity
public class ApplicationUser : IdentityUser
{
    [PersonalData]
    public string? FirstName { get; set; }

    [PersonalData]
    public string? LastName { get; set; }
}
```

```csharp
// ✓ PII nikdy v URL — jen v těle požadavku nebo šifrovaných tokenech
// ✗ GET /api/users?email=jan@example.com  (URL se loguje)
// ✓ POST /api/users/search s tělem { "email": "..." }

// ✓ Tokenizace pro emailové odkazy (reset hesla, potvrzení)
var token = _dataProtector.Protect(
    JsonSerializer.Serialize(new { UserId = userId, Expires = DateTime.UtcNow.AddHours(1) }));
var link = $"{_baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
```
