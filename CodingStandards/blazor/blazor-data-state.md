# Blazor — Data, DI scope & stav

## Klíčový rozdíl: Scoped = celý okruh

V Interactive Server **neexistuje scope per request**. `Scoped` služba žije po celou dobu okruhu (minuty až hodiny), sdílí ji všechny komponenty uživatele.

Důsledky:
- `DbContext` (scoped) by žil celý okruh → rostoucí change tracker, zastaralá data, **souběžné operace** (`A second operation was started on this context`).
- Command/query handlery z `cqrs/` (scoped, injektují `AppDbContext`) mají stejný problém.

---

## Řešení A — `IDbContextFactory` (jednoduché stránky)

```csharp
builder.Services.AddDbContextFactory<AppDbContext>(o => o.UseSqlServer(connectionString));
```

```razor
@inject IDbContextFactory<AppDbContext> DbFactory

@code {
    private async Task LoadAsync()
    {
        await using var db = await DbFactory.CreateDbContextAsync(_cts.Token);
        _products = await db.Products.AsNoTracking()
            .Select(p => new ProductRow(p.Id, p.Name, p.Price))
            .ToListAsync(_cts.Token);
    }
}
```

✗ `@inject AppDbContext Db` v komponentě.

---

## Řešení B — scope na operaci (CQRS handlery)

Komponenta volá handlery přes dispatcher, který vytvoří **nový DI scope pro každou operaci** a přenese do něj aktuálního uživatele.

```csharp
// Settable ICurrentUser — v API ho plní middleware z HttpContext.User, v Blazoru dispatcher
public sealed class CurrentUser : ICurrentUser
{
    public ClaimsPrincipal Principal { get; set; } = new();
    public string? Id => Principal.FindFirstValue(ClaimTypes.NameIdentifier);
    public string? Name => Principal.Identity?.Name;
}
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUser>());

// Scoped = žije v okruhu; každé volání vytvoří krátký scope
public sealed class BlazorDispatcher(IServiceScopeFactory scopes, AuthenticationStateProvider auth)
{
    public async Task<TResult> SendAsync<TCommand, TResult>(TCommand command, CancellationToken ct)
    {
        await using var scope = await CreateScopeAsync();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<TCommand, TResult>>();
        return await handler.HandleAsync(command, ct);
    }

    public async Task<TResult> QueryAsync<TQuery, TResult>(TQuery query, CancellationToken ct)
    {
        await using var scope = await CreateScopeAsync();
        var handler = scope.ServiceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        return await handler.HandleAsync(query, ct);
    }

    private async Task<AsyncServiceScope> CreateScopeAsync()
    {
        var state = await auth.GetAuthenticationStateAsync();
        var scope = scopes.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentUser>().Principal = state.User;
        return scope;
    }
}
builder.Services.AddScoped<BlazorDispatcher>();
```

```razor
@inject BlazorDispatcher Dispatcher

var result = await Dispatcher.SendAsync<ConfirmOrderCommand, Result<OrderId>>(new(OrderId), _cts.Token);
```

- ⚠ V novém scope **nejsou** okruhové služby (`AuthenticationStateProvider`, `NavigationManager`, `IJSRuntime`) — handlery na nich nesmí záviset. Proto `ICurrentUser`.
- ✗ `HttpContextCurrentUser` (viz `database-ef/`) v interaktivních komponentách **nefunguje** — `HttpContext` je null nebo patří úvodnímu requestu.

---

## Uživatel v komponentě

```razor
@* ✓ Cascading AuthenticationState (registruj AddCascadingAuthenticationState()) *@
[CascadingParameter] private Task<AuthenticationState> AuthState { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    var user = (await AuthState).User;
    _canEdit = user.IsInRole("OrderManager");
}
```

```csharp
builder.Services.AddCascadingAuthenticationState();
// ✗ IHttpContextAccessor v komponentách
```

---

## Stav aplikace

| Stav | Kde držet |
|------|-----------|
| Stav jedné komponenty | Pole v komponentě |
| Sdílený mezi komponentami jednoho uživatele | Scoped state container (žije v okruhu) |
| Musí přežít obnovení stránky / odpojení | URL (query string), DB, `ProtectedSessionStorage` |
| Sdílený mezi uživateli | Singleton + thread-safe (`ConcurrentDictionary`, `lock`), nebo DB / cache |

```csharp
// Scoped state container s notifikací
public sealed class CartState
{
    private readonly List<CartItem> _items = [];
    public IReadOnlyList<CartItem> Items => _items;
    public event Action? Changed;

    public void Add(CartItem item) { _items.Add(item); Changed?.Invoke(); }
}
// Komponenty: Changed += () => InvokeAsync(StateHasChanged); a odhlásit v DisposeAsync!
```

- ✗ **Statická pole se stavem** — sdílí se mezi všemi uživateli (únik dat mezi uživateli!).
- ✗ Singleton s daty konkrétního uživatele.
- Filtry, stránka, řazení seznamu patří do URL (`[SupplyParameterFromQuery]`) — sdílitelné odkazy, přežijí reload.
- `ProtectedLocalStorage` / `ProtectedSessionStorage` jsou šifrované, ale dostupné až po prerenderingu (JS interop).

---

## Real-time aktualizace

Okruh je už SignalR spojení — pro notifikace mezi uživateli **není potřeba vlastní hub**.

```csharp
// Singleton notifier — služba po uložení vyvolá událost, komponenty ostatních uživatelů se překreslí
public sealed class StockNotifier
{
    public event Action<StockChanged>? StockChanged;
    public void Notify(StockChanged e) => StockChanged?.Invoke(e);
    public int SubscriberCount => StockChanged?.GetInvocationList().Length ?? 0;   // diagnostika leaků, testy
}
```

⚠ Strop: funguje jen v rámci **jedné instance** serveru. Při více instancích → Redis pub/sub nebo broker (viz `event-driven/`).
