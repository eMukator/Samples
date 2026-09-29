# Observability — Health Checks, Metriky & Tracing

## Health Checks — kompletní setup

```bash
dotnet add package AspNetCore.HealthChecks.SqlServer
dotnet add package AspNetCore.HealthChecks.Redis
dotnet add package AspNetCore.HealthChecks.Uris
dotnet add package AspNetCore.HealthChecks.UI.Client
```

```csharp
// Program.cs
builder.Services
    .AddHealthChecks()

    // Databáze
    .AddSqlServer(
        connectionString: config.GetConnectionString("Default")!,
        healthQuery:      "SELECT 1",
        name:             "database",
        failureStatus:    HealthStatus.Unhealthy,
        tags:             ["db", "ready"])

    // Cache
    .AddRedis(
        redisConnectionString: config.GetConnectionString("Redis")!,
        name:   "redis",
        tags:   ["cache", "ready"])

    // Disk
    .AddDiskStorageHealthCheck(setup =>
        setup.AddDrive("C:\\", minimumFreeMegabytes: 500),
        name: "disk",
        tags: ["infrastructure"])

    // Vlastní check
    .AddCheck<ExternalApiHealthCheck>("external-api", tags: ["external", "ready"]);
```

```csharp
// ExternalApiHealthCheck.cs — vlastní health check
public class ExternalApiHealthCheck(HttpClient httpClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var response = await httpClient.GetAsync("/health", cts.Token);
            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("External API is responsive")
                : HealthCheckResult.Degraded($"External API returned {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("External API is unreachable", ex);
        }
    }
}
```

```csharp
// Endpointy
// /health/live  — jen že aplikace běží (Kubernetes liveness probe)
// /health/ready — včetně závislostí (Kubernetes readiness probe)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false,   // žádné kontroly — jen HTTP 200
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate      = check => check.Tags.Contains("ready"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).AllowAnonymous();

app.MapHealthChecks("/health/full", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
}).RequireAuthorization("AdminOnly");
```

```json
// Response /health/ready
{
  "status": "Healthy",
  "totalDuration": "00:00:00.1523",
  "entries": {
    "database": {
      "status": "Healthy",
      "duration": "00:00:00.0823",
      "tags": ["db", "ready"]
    },
    "redis": {
      "status": "Healthy",
      "duration": "00:00:00.0045",
      "tags": ["cache", "ready"]
    }
  }
}
```

---

## Vlastní Metriky — .NET Meters API

```csharp
// OrderMetrics.cs — centralizované metriky
public class OrderMetrics : IDisposable
{
    private readonly Meter _meter;

    // Counters — monotónně rostoucí hodnoty
    private readonly Counter<long>     _ordersCreated;
    private readonly Counter<long>     _ordersFailed;
    private readonly Counter<decimal>  _revenueTotal;

    // Histograms — distribuce hodnot (latence, velikosti)
    private readonly Histogram<double> _orderProcessingTime;
    private readonly Histogram<double> _orderTotal;

    // Gauges — aktuální hodnota (fronta, připojení)
    private readonly ObservableGauge<int> _pendingOrdersGauge;

    public OrderMetrics(IMeterFactory meterFactory, IOrderRepository repository)
    {
        _meter = meterFactory.Create("MyApp.Orders");

        _ordersCreated = _meter.CreateCounter<long>(
            "orders.created.total",
            description: "Celkový počet vytvořených objednávek");

        _ordersFailed = _meter.CreateCounter<long>(
            "orders.failed.total",
            description: "Celkový počet neúspěšných objednávek");

        _revenueTotal = _meter.CreateCounter<decimal>(
            "revenue.total.czk",
            unit:        "CZK",
            description: "Celkový příjem");

        _orderProcessingTime = _meter.CreateHistogram<double>(
            "orders.processing.duration",
            unit:        "ms",
            description: "Čas zpracování objednávky");

        _orderTotal = _meter.CreateHistogram<double>(
            "orders.total.czk",
            unit:        "CZK",
            description: "Distribuce hodnot objednávek");

        _pendingOrdersGauge = _meter.CreateObservableGauge(
            "orders.pending.count",
            () => repository.GetPendingCountSync(),
            description: "Počet čekajících objednávek");
    }

    public void RecordOrderCreated(Order order)
    {
        var tags = new TagList
        {
            { "status",   order.Status.ToString() },
            { "channel",  order.Channel },
            { "currency", "CZK" }
        };

        _ordersCreated.Add(1, tags);
        _revenueTotal.Add((long)order.Total, tags);
        _orderTotal.Record((double)order.Total, tags);
    }

    public void RecordProcessingTime(double milliseconds, string status)
        => _orderProcessingTime.Record(milliseconds,
            new TagList { { "outcome", status } });

    public void Dispose() => _meter.Dispose();
}

// Program.cs
builder.Services.AddSingleton<OrderMetrics>();
builder.Services.AddMetrics();
```

---

## Distributed Tracing — OpenTelemetry

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
dotnet add package OpenTelemetry.Instrumentation.Http
dotnet add package OpenTelemetry.Exporter.Otlp  # pro Jaeger, Tempo, etc.
```

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("MyApp", serviceVersion: "1.0.0"))

    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation(options =>
        {
            options.Filter = ctx =>
                !ctx.Request.Path.StartsWithSegments("/health");  // neignoruj health checks v traces
            options.RecordException = true;
        })
        .AddEntityFrameworkCoreInstrumentation(options =>
        {
            options.SetDbStatementForText = true;  // loguj SQL (jen ve vývoji!)
        })
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(options =>
            options.Endpoint = new Uri(config["OpenTelemetry:Endpoint"]!)))

    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MyApp.Orders")          // vlastní metriky
        .AddPrometheusExporter());         // /metrics endpoint

// Prometheus endpoint
app.MapPrometheusScrapingEndpoint("/metrics")
   .RequireAuthorization("MonitoringOnly");
```

```csharp
// Vlastní Activity (span) pro business operace
private static readonly ActivitySource _activitySource = new("MyApp.Orders");

public async Task<OrderDto> ProcessOrderAsync(int orderId, CancellationToken ct)
{
    using var activity = _activitySource.StartActivity("ProcessOrder");
    activity?.SetTag("order.id", orderId);

    try
    {
        var order = await _repository.GetByIdAsync(orderId, ct);
        activity?.SetTag("order.customer", order?.CustomerName);

        // ... zpracování

        activity?.SetStatus(ActivityStatusCode.Ok);
        return MapToDto(order!);
    }
    catch (Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.RecordException(ex);
        throw;
    }
}
```
