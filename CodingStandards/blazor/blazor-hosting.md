# Blazor — Hosting, okruhy & škálování

## Okruh (circuit)

Každá otevřená záložka = jeden okruh = stav v paměti serveru + WebSocket spojení.

- Zdroje rostou lineárně s počtem **současně připojených** uživatelů, ne s počtem requestů.
- Zátěžově otestuj počet souběžných okruhů před nasazením (paměť na okruh závisí na velikosti stavu komponent).

---

## Více instancí

| Nasazení | Požadavek |
|----------|-----------|
| Jedna instance | Nic navíc |
| Více instancí (load balancer) | **Sticky sessions** (session affinity) — okruh žije na konkrétním serveru |
| Azure App Service, škálování | Azure SignalR Service (`AddAzureSignalR()`) nebo ARR affinity |
| Kubernetes / reverse proxy | Affinity podle cookie + povolené WebSockets, `proxy_read_timeout` delší než keep-alive |

✗ Bez affinity: náhodná odpojení a „ztracený stav".

### Reverse proxy (nginx)

```nginx
location / {
    proxy_pass http://app;
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection $connection_upgrade;
    proxy_read_timeout 120s;
}
```

Bez WebSocketů SignalR spadne na long polling — funkční, ale výrazně pomalejší. Zkontroluj v DevTools → Network → WS.

---

## Odpojení a obnova

- Krátké odpojení → klient se sám připojí zpět ke stejnému okruhu (pokud ještě žije v `DisconnectedCircuitRetentionPeriod`).
- Okruh vypršel / server restartoval → stav je **ztracen**, uživatel musí stránku obnovit.
- **.NET 10+**: stav okruhu lze persistovat při odpojení (`[PersistentState]` property se obnoví do nového okruhu) — využij pro rozpracované formuláře.

### Reconnect UI

Výchozí šablona (.NET 10: komponenta `ReconnectModal`) — přizpůsob vzhledu aplikace a texty přelož.

```css
/* Stavy elementu #components-reconnect-modal */
.components-reconnect-show     { /* pokus o spojení */ }
.components-reconnect-failed   { /* nabídni tlačítko „Obnovit" */ }
.components-reconnect-rejected { /* okruh neexistuje — nutný reload */ }
```

✓ Uživateli vždy dej jasnou akci („Obnovit stránku"), ne nekonečný spinner.

---

## Monitoring okruhů

```csharp
public sealed class CircuitMetricsHandler(ILogger<CircuitMetricsHandler> logger) : CircuitHandler
{
    private static int _active;

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        var count = Interlocked.Increment(ref _active);
        logger.LogDebug("Circuit {CircuitId} opened, active {ActiveCircuits}", circuit.Id, count);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        Interlocked.Decrement(ref _active);
        return Task.CompletedTask;
    }
}
builder.Services.AddScoped<CircuitHandler, CircuitMetricsHandler>();
```

- SignalR metriky (od .NET 8) a Blazor metriky okruhů (od .NET 10) jdou přes `System.Diagnostics.Metrics` → OpenTelemetry (viz `observability/`) — na .NET 10+ preferuj je před vlastním počítadlem.
- Sleduj: aktivní okruhy, odpojené okruhy, paměť procesu, latenci (round-trip) — latence > ~200 ms je pro uživatele znatelná.

---

## Graceful shutdown a deploy

- Deploy = restart = **všichni připojení uživatelé ztratí stav**. Plánuj nasazení mimo špičku, nebo rolling deploy s affinity.
- `HostOptions.ShutdownTimeout` dostatečně dlouhý na dokončení probíhajících operací.
- Health checks `/live` + `/ready` (viz `environment-config/`, `observability/`).
