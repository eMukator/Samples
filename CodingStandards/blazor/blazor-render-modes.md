# Blazor — Render modes & prerendering

## Registrace

```csharp
// Program.cs
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

app.UseAntiforgery();
app.MapStaticAssets();                       // .NET 9+
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
```

---

## Globální vs. per-page interaktivita

| Aplikace | Nastavení |
|----------|-----------|
| Interní / admin aplikace, vše interaktivní, žádné SEO | **Globálně** v `App.razor` |
| Veřejný web s interaktivními částmi | Static SSR výchozí, `@rendermode InteractiveServer` jen na stránkách / komponentách, které to potřebují |

```razor
@* App.razor — globální interaktivita *@
<HeadOutlet @rendermode="InteractiveServer" />
...
<Routes @rendermode="InteractiveServer" />
```

```razor
@* Per-page *@
@page "/orders/{Id:guid}"
@rendermode InteractiveServer
```

- Stránky ASP.NET Core Identity (`/Account/*`) musí běžet jako **static SSR** — potřebují `HttpContext` pro cookies. Šablona to řeší přes `HttpContext.AcceptsInteractiveRouting()` v `App.razor`; neodstraňuj to.
- Interaktivní komponenta nemůže obsahovat komponentu s **jiným** interaktivním render mode.
- Parametry předávané z SSR do interaktivní komponenty musí být serializovatelné (žádné delegáty, `RenderFragment`).

---

## Prerendering — dvojí inicializace

Ve výchozím stavu se interaktivní komponenta nejdřív **prerenderuje** (statické HTML), pak se znovu vytvoří v okruhu. `OnInitializedAsync` proběhne **dvakrát**.

```razor
@* ✗ Špatně — dva dotazy do DB, blikání obsahu *@
@code {
    private OrderDetailDto? _order;
    protected override async Task OnInitializedAsync()
        => _order = await Queries.GetOrderDetailAsync(Id);
}
```

```razor
@* ✓ .NET 10+ — deklarativní persistence stavu z prerenderingu *@
@code {
    [PersistentState]
    public OrderDetailDto? Order { get; set; }

    protected override async Task OnInitializedAsync()
        => Order ??= await Queries.GetOrderDetailAsync(Id);
}
```

```razor
@* ✓ .NET 8/9 — PersistentComponentState *@
@inject PersistentComponentState AppState
@implements IDisposable
@code {
    private OrderDetailDto? _order;
    private PersistingComponentStateSubscription _subscription;

    protected override async Task OnInitializedAsync()
    {
        _subscription = AppState.RegisterOnPersisting(() =>
        {
            AppState.PersistAsJson(nameof(_order), _order);
            return Task.CompletedTask;
        });

        if (!AppState.TryTakeFromJson<OrderDetailDto>(nameof(_order), out _order))
            _order = await Queries.GetOrderDetailAsync(Id);
    }

    public void Dispose() => _subscription.Dispose();
}
```

Alternativa — vypnout prerendering (stránka bez SEO, např. administrace):

```razor
@rendermode @(new InteractiveServerRenderMode(prerender: false))
```

---

## Zjištění stavu renderování (.NET 9+)

```razor
@if (!RendererInfo.IsInteractive)
{
    <p>Načítání…</p>     @* prerender — tlačítka ještě nefungují *@
}

<button disabled="@(!RendererInfo.IsInteractive)" @onclick="SaveAsync">Uložit</button>
```

✓ Během prerenderingu zakaž interaktivní prvky — klik před připojením okruhu se ztratí.

---

## JS interop až po vykreslení

`IJSRuntime` během prerenderingu **nefunguje** (není spojení s prohlížečem).

```csharp
// ✗ Špatně — výjimka při prerenderingu
protected override async Task OnInitializedAsync()
    => await JS.InvokeVoidAsync("initMap");

// ✓ Správně
private IJSObjectReference? _module;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (!firstRender) return;
    _module = await JS.InvokeAsync<IJSObjectReference>("import", "./Components/Map.razor.js");
    await _module.InvokeVoidAsync("init", _mapElement);
}

public async ValueTask DisposeAsync()
{
    try { if (_module is not null) await _module.DisposeAsync(); }
    catch (JSDisconnectedException) { }   // okruh už je odpojený — v pořádku
}
```

- JS ke komponentě jako kolokovaný modul `Component.razor.js`, ne globální skripty.
- Data přijatá z JS jsou **nedůvěryhodný vstup** — validuj.
