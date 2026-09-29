# Environment Konfigurace — appsettings & Options

## appsettings hierarchie

```
Pořadí načítání (každý přepíše předchozí):
1. appsettings.json                    ← výchozí hodnoty, commitnout
2. appsettings.{Environment}.json      ← environment override, commitnout (bez secrets)
3. ~/.microsoft/usersecrets/           ← dev secrets, MIMO repo
4. Environment Variables               ← produkční konfigurace
5. Azure Key Vault / AWS Secrets       ← produkční secrets
```

```json
// appsettings.json — commitnout, žádné secrets
{
  "App": {
    "Name":    "MyApp",
    "Version": "1.0",
    "BaseUrl": "https://example.com"
  },
  "Features": {
    "EnableNewDashboard": false,
    "MaintenanceMode":    false,
    "MaxUploadSizeMb":    10
  },
  "Pagination": {
    "DefaultPageSize": 20,
    "MaxPageSize":     100
  },
  "Cache": {
    "DefaultExpirationMinutes": 60,
    "SlidingExpirationMinutes": 15
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning"
      }
    }
  }
}
```

```json
// appsettings.Development.json — commitnout
{
  "App": {
    "BaseUrl": "https://localhost:7001"
  },
  "Features": {
    "EnableNewDashboard": true
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft.EntityFrameworkCore.Database.Command": "Information"
      }
    }
  }
}
```

```json
// appsettings.Testing.json — commitnout
{
  "Features": {
    "MaintenanceMode": false
  },
  "Serilog": {
    "MinimumLevel": { "Default": "Warning" }
  }
}
```

## Strongly-typed options — kompletní vzor

```csharp
// Options třídy — record pro immutability
public record AppOptions
{
    [Required]
    public string Name { get; init; } = default!;

    [Required, Url]
    public string BaseUrl { get; init; } = default!;

    public string Version { get; init; } = "1.0";
}

public record FeaturesOptions
{
    public bool EnableNewDashboard { get; init; }
    public bool MaintenanceMode    { get; init; }

    [Range(1, 100)]
    public int MaxUploadSizeMb { get; init; } = 10;
}

public record PaginationOptions
{
    [Range(1, 100)]
    public int DefaultPageSize { get; init; } = 20;

    [Range(1, 500)]
    public int MaxPageSize { get; init; } = 100;
}

public record EmailOptions
{
    [Required, EmailAddress]
    public string FromAddress { get; init; } = default!;

    [Required]
    public string FromName { get; init; } = default!;

    [Required]
    public string SmtpHost { get; init; } = default!;

    [Range(1, 65535)]
    public int SmtpPort { get; init; } = 587;

    public bool UseSsl { get; init; } = true;

    // Secrets — z User Secrets nebo env vars
    [Required]
    public string SmtpUser { get; init; } = default!;

    [Required]
    public string SmtpPassword { get; init; } = default!;
}
```

```csharp
// Program.cs — registrace s validací při startu
void AddOptions<TOptions>(string section) where TOptions : class
    => builder.Services
        .AddOptions<TOptions>()
        .BindConfiguration(section)
        .ValidateDataAnnotations()
        .ValidateOnStart();  // spadne při startu pokud konfigurace chybí

AddOptions<AppOptions>("App");
AddOptions<FeaturesOptions>("Features");
AddOptions<PaginationOptions>("Pagination");
AddOptions<EmailOptions>("Email");
```

```csharp
// Použití v services — IOptions<T> pro neměnné, IOptionsSnapshot<T> pro per-request
public class OrderService(
    IOptions<PaginationOptions> pagination,
    IOptionsSnapshot<FeaturesOptions> features)  // snapshot = aktualizuje se per-request
{
    public async Task<PagedResponse<OrderDto>> GetPagedAsync(int? size, CancellationToken ct)
    {
        var pageSize = Math.Min(
            size ?? pagination.Value.DefaultPageSize,
            pagination.Value.MaxPageSize);

        if (features.Value.MaintenanceMode)
            throw new ServiceUnavailableException("Systém je v údržbě.");

        // ...
    }
}
```

## Environment Variables — konvence

```bash
# Separator pro vnořené klíče: __ (dvojité podtržítko)
# ConnectionStrings:Default → ConnectionStrings__Default

# Docker / docker-compose
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Server=prod-db;Database=MyApp;User=app;Password=...
Email__SmtpHost=smtp.sendgrid.net
Email__SmtpPort=587
Email__SmtpUser=apikey
Email__SmtpPassword=SG.xxxxx
Jwt__Secret=production-secret-min-32-characters-random
```

```yaml
# docker-compose.yml — env file pro lokální vývoj
services:
  web:
    env_file: .env.local  # gitignorovaný soubor
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
```

```bash
# .env.local (gitignorovat!)
ConnectionStrings__Default=Server=localhost;Database=MyApp;...
Email__SmtpPassword=dev-password
Jwt__Secret=dev-secret-min-32-characters-local
```

## Startup validace — ověř kritické závislosti

```csharp
// StartupValidationService.cs
public class StartupValidationService(
    IConfiguration config,
    ILogger<StartupValidationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        var errors = new List<string>();

        // Ověř DB spojení
        var connStr = config.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connStr))
            errors.Add("Chybí ConnectionStrings:Default");

        // Ověř JWT secret je dostatečně dlouhý
        var jwtSecret = config["Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
            errors.Add("Jwt:Secret musí mít alespoň 32 znaků");

        if (errors.Count > 0)
        {
            foreach (var error in errors)
                logger.LogCritical("Chyba konfigurace: {Error}", error);

            throw new InvalidOperationException(
                $"Kritické chyby konfigurace:\n{string.Join("\n", errors)}");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}

// Program.cs
builder.Services.AddHostedService<StartupValidationService>();
```
