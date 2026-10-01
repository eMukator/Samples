# Blazor — Komponenty

## Lifecycle — co kam patří

| Metoda | Použití |
|--------|---------|
| `OnInitializedAsync` | Jednorázová inicializace nezávislá na parametrech |
| `OnParametersSetAsync` | Načtení dat podle parametrů (route, `[Parameter]`) — volá se při **každé** změně |
| `OnAfterRenderAsync(firstRender)` | JS interop, práce s `ElementReference` |
| `DisposeAsync` | Úklid: odběry, timery, `CancellationTokenSource`, JS moduly |

```razor
@* ✗ Špatně — navigace /orders/1 → /orders/2 komponentu NEVYTVOŘÍ znovu, data zůstanou stará *@
protected override async Task OnInitializedAsync() => _order = await LoadAsync(Id);

@* ✓ Správně *@
protected override async Task OnParametersSetAsync()
{
    if (_order?.Id == Id) return;   // parametr se nezměnil
    _order = await LoadAsync(Id);
}
```

---

## Event handlery

```razor
@* ✓ Async handler vrací Task — Blazor po dokončení sám zavolá StateHasChanged *@
<button @onclick="SaveAsync" disabled="@_isSaving">Uložit</button>

@code {
    private bool _isSaving;

    private async Task SaveAsync()
    {
        _isSaving = true;                // zabrání dvojkliku = dvojímu zápisu
        try { await ... }
        finally { _isSaving = false; }
    }
}
```

- ✗ Nikdy `async void` handler — výjimka shodí okruh bez možnosti ošetření.
- Neošetřená výjimka v handleru / lifecycle metodě **ukončí celý okruh** (uživatel ztratí stav). Očekávané chyby ošetři a zobraz; neočekávané zachytí `ErrorBoundary`.

```razor
@* MainLayout.razor *@
<ErrorBoundary @ref="_errorBoundary">
    <ChildContent>@Body</ChildContent>
    <ErrorContent>
        <div role="alert">Něco se pokazilo. <button @onclick="() => _errorBoundary?.Recover()">Zkusit znovu</button></div>
    </ErrorContent>
</ErrorBoundary>

@code {
    private ErrorBoundary? _errorBoundary;
    protected override void OnParametersSet() => _errorBoundary?.Recover();   // reset při navigaci
}
```

---

## Aktualizace z jiného vlákna

Timer, událost ze služby, `Channel`, SignalR hub → callback neběží v synchronizačním kontextu komponenty.

```csharp
// ✗ Špatně — InvalidOperationException / race condition
private void OnStockChanged(StockChanged e) { _stock = e.Quantity; StateHasChanged(); }

// ✓ Správně
private void OnStockChanged(StockChanged e) => _ = InvokeAsync(() =>
{
    _stock = e.Quantity;
    StateHasChanged();
});
```

---

## Dispose — prevence memory leaků

Okruh žije dlouho. Neodhlášený odběr drží komponentu (a celý její strom) v paměti serveru.

```razor
@implements IAsyncDisposable
@inject StockNotifier Notifier

@code {
    private readonly CancellationTokenSource _cts = new();
    private PeriodicTimer? _timer;

    protected override void OnInitialized()
    {
        Notifier.StockChanged += OnStockChanged;
        _ = PollAsync(_cts.Token);
    }

    private async Task PollAsync(CancellationToken ct)
    {
        _timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await _timer.WaitForNextTickAsync(ct))
                await InvokeAsync(RefreshAsync);
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        Notifier.StockChanged -= OnStockChanged;
        await _cts.CancelAsync();
        _cts.Dispose();
        _timer?.Dispose();
    }
}
```

`_cts.Token` předávej i do všech async volání komponenty — při odchodu ze stránky se zruší rozběhnuté dotazy.

---

## Parametry a komunikace

```razor
@* ✗ Špatně — potomek přepisuje vlastní [Parameter]; rodič ho při dalším renderu přepíše zpět *@
[Parameter] public int Quantity { get; set; }
private void Increment() => Quantity++;

@* ✓ Správně — stav vlastní rodič, potomek hlásí změnu *@
[Parameter] public int Quantity { get; set; }
[Parameter] public EventCallback<int> QuantityChanged { get; set; }
private Task Increment() => QuantityChanged.InvokeAsync(Quantity + 1);

@* Rodič: <QuantityPicker @bind-Quantity="line.Quantity" /> *@
```

- `[Parameter]` jen `{ get; set; }` bez logiky v setteru.
- `[EditorRequired]` u povinných parametrů.
- `CascadingValue` jen pro opravdu průřezová data (téma, uživatel), s `IsFixed="true"`, pokud se nemění.

---

## Výkon renderování

```razor
@* ✓ @key — stabilní identita prvků při změně pořadí / mazání *@
@foreach (var line in _order.Lines)
{
    <OrderLineRow @key="line.Id" Line="line" />
}

@* ✓ Virtualize — renderuje jen viditelné řádky *@
<Virtualize Items="products" Context="p" ItemSize="48">
    <ProductRow Product="p" />
</Virtualize>

@* ✓ QuickGrid — tabulky s řazením a stránkováním; s IQueryable přes EF adapter *@
<QuickGrid ItemsProvider="provider" Pagination="pagination">
    <PropertyColumn Property="o => o.Number" Title="Číslo" Sortable="true" />
    <PropertyColumn Property="o => o.Total" Format="N2" Title="Celkem" />
</QuickGrid>
<Paginator State="pagination" />
```

- Každý render = diff poslaný přes síť. Velké komponenty rozděl, aby se překreslovaly jen změněné části.
- `@bind:event="oninput"` posílá round-trip na **každý znak** — u vyhledávání přidej debounce (`@bind:after` + `Task.Delay` s cancel) nebo použij `onchange`.
- Žádné drahé výpočty v markupu (`@items.Where(...).OrderBy(...)` se počítá při každém renderu) — počítej v kódu po změně dat.

---

## Organizace

```
Components/
  Layout/
  Pages/Orders/
    OrderDetail.razor
    OrderDetail.razor.cs      ← code-behind, když @code > ~50 řádků
    OrderDetail.razor.css     ← CSS isolation (nebo Tailwind, viz tailwind/)
    OrderDetail.razor.js      ← kolokovaný JS modul
  Shared/
```

- Stránka (`@page`) orchestruje, prezentační komponenty jsou „hloupé" (parametry + `EventCallback`).
- Business logika nepatří do komponent — volej command/query handlery (viz `cqrs/`, `blazor-data-state.md`).
