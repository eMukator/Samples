# Bezpečnost — Správa Secrets

## Kde secrets NESMÍ být

```csharp
// ✗ NIKDY v appsettings.json commitnutém do gitu
{
  "ConnectionStrings": {
    "Default": "Server=prod;Password=SuperSecret123"
  },
  "Jwt": { "Secret": "moje-tajne-heslo" }
}

// ✗ NIKDY přímo v kódu
var connStr = "Server=prod;Password=SuperSecret123";
private const string ApiKey = "sk-abc123...";

// ✗ NIKDY v proměnných prostředí commitnutých v .env souborech do gitu
```

## Správné umístění secrets

### Vývoj — .NET User Secrets

```bash
# Inicializace (jednorázově)
dotnet user-secrets init --project src/MyApp.Web

# Nastavení secrets
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost;..."
dotnet user-secrets set "Jwt:Secret" "lokalni-dev-secret-min-32-znaku"
dotnet user-secrets set "ExternalApi:Key" "dev-api-key"
```

```csharp
// Program.cs — User Secrets se načtou automaticky v Development
// Není třeba explicitní volání v moderním .NET
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
    .AddUserSecrets<Program>(optional: true)  // jen Development
    .AddEnvironmentVariables();
```

### Produkce — Environment Variables nebo Azure Key Vault

```csharp
// Program.cs — Azure Key Vault
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUri = builder.Configuration["KeyVault:Uri"]!;
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUri),
        new DefaultAzureCredential());  // Managed Identity — žádné heslo!
}
```

```bash
# Produkce — Environment Variables (Docker / Azure App Service)
ConnectionStrings__Default=Server=prod;Password=...
Jwt__Secret=produkční-secret-min-32-znaku-náhodný
```

## Strongly-typed konfigurace s validací

```csharp
// JwtOptions.cs
public record JwtOptions
{
    [Required, MinLength(32)]
    public string Secret { get; init; } = default!;

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    public int ExpirationMinutes { get; init; } = 15;
}

// Program.cs — validace při startu aplikace
builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration("Jwt")
    .ValidateDataAnnotations()
    .ValidateOnStart();  // spadne při startu pokud chybí povinná hodnota
```

## .gitignore — povinné záznamy

```gitignore
# Secrets
*.env
.env.*
secrets.json
appsettings.Local.json

# .NET User Secrets
**/Properties/launchSettings.json  # může obsahovat connection strings
```

## Audit — kontrola úniku secrets

```bash
# Zkontroluj historii gitu na úniky
git log --all --full-history -- "**appsettings*"

# Nástroj pro skenování
dotnet tool install --global trufflehog  # nebo gitleaks
gitleaks detect --source . --verbose
```
