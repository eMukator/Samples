# Testování — Blazor komponenty (bUnit)

Komponenty Interactive Serveru testuj **bUnit**em — renderuje komponentu v paměti, bez prohlížeče a SignalR. Pravidla komponent viz `blazor/`.

```bash
dotnet add package bunit
```

> Příklady jsou pro bUnit 2.x (`BunitContext`, `Render`). Ve verzi 1.x se třída jmenuje `TestContext` a metoda `RenderComponent`.

## Co bUnitem testovat

| Ano | Ne (patří jinam) |
|-----|------------------|
| Co komponenta vykreslí pro daný stav / parametry | Business pravidla → doménové testy |
| Reakce na kliknutí, vstup, submit | Dotazy do DB → integrační testy |
| Volání handleru / dispatcheru se správným commandem | Vzhled, CSS, layout → E2E / vizuální testy |
| Zobrazení chyby z `Result` | Celý průchod aplikací → Playwright (`test-builders-e2e.md`) |
| Autorizované / neautorizované zobrazení | |
| Navigace po úspěchu | |

---

## Základ testu

```csharp
public sealed class OrderFormTests : BunitContext
{
    private readonly RecordingHandler<CreateOrderCommand, Result<OrderId>> _handler = new();

    public OrderFormTests()
    {
        // Komponenta volá BlazorDispatcher → ten resolvuje handler z DI (blazor/blazor-data-state.md)
        Services.AddScoped<ICommandHandler<CreateOrderCommand, Result<OrderId>>>(_ => _handler);
        Services.AddScoped<CurrentUser>();
        Services.AddScoped<BlazorDispatcher>();
        AddAuthorization().SetAuthorized("jan.novak");
    }

    [Fact]
    public void Submit_ValidForm_SendsCommandAndNavigates()
    {
        var newId = OrderId.New();
        _handler.Result = newId;
        var cut = Render<OrderForm>();

        cut.Find("#email").Change("jan@example.com");
        cut.Find("#qty").Change("2");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => _handler.Calls.Should().Be(1));
        Services.GetRequiredService<BunitNavigationManager>().Uri.Should().EndWith($"/orders/{newId}");
    }

    [Fact]
    public void Submit_BusinessError_ShowsMessageAndStaysOnPage()
    {
        _handler.Result = Error.Conflict("Zboží není skladem.");
        var cut = Render<OrderForm>();

        cut.Find("#email").Change("jan@example.com");
        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.Should().Contain("není skladem"));
    }

    [Fact]
    public void Submit_InvalidForm_DoesNotCallHandler()
    {
        var cut = Render<OrderForm>();

        cut.Find("form").Submit();   // prázdný e-mail

        cut.FindAll(".validation-message").Should().NotBeEmpty();
        _handler.Calls.Should().Be(0);
    }
}
```

- Handlery nahrazuj **fake / recording** implementací (viz `test-unit-layers.md`), ne NSubstitute — stejné pravidlo jako všude.
- Elementy hledej podle stabilních selektorů: `id`, `role`, `data-testid`, `label`. ✗ Podle CSS tříd (Tailwind se mění).
- Asynchronní změny (await v handleru) ověřuj přes `WaitForAssertion` / `WaitForState`, ne hned po kliknutí.

---

## Parametry a EventCallback

```csharp
[Fact]
public void Increment_RaisesQuantityChanged()
{
    var reported = 0;
    var cut = Render<QuantityPicker>(p => p
        .Add(x => x.Quantity, 1)
        .Add(x => x.QuantityChanged, (int q) => reported = q));

    cut.Find("button[aria-label='Přidat']").Click();

    reported.Should().Be(2);
    cut.Instance.Quantity.Should().Be(1);   // komponenta nemění vlastní [Parameter] (blazor-components.md)
}
```

---

## Autorizace

```csharp
[Fact]
public void DeleteButton_UserWithoutPolicy_IsNotRendered()
{
    AddAuthorization().SetAuthorized("jan.novak");   // bez policy Orders.Manage
    var cut = Render<OrderDetail>(p => p.Add(x => x.Id, Guid.NewGuid()));

    cut.FindAll("[data-testid=delete-order]").Should().BeEmpty();
}

[Fact]
public void DeleteButton_Manager_IsRendered()
{
    AddAuthorization().SetAuthorized("eva").SetPolicies("Orders.Manage");
    var cut = Render<OrderDetail>(p => p.Add(x => x.Id, Guid.NewGuid()));

    cut.Find("[data-testid=delete-order]");   // Find vyhodí výjimku, pokud element chybí
}
```

⚠ Tento test ověřuje jen **UX**. Že handler odmítne neoprávněného uživatele, testuj v testu handleru (`blazor/blazor-forms-security.md` — autorizace v handleru).

---

## JS interop

```csharp
private readonly BunitJSModuleInterop _module;

public MapComponentTests()
{
    _module = JSInterop.SetupModule("./Components/Map.razor.js");
    _module.SetupVoid("init", _ => true);
}

[Fact]
public void FirstRender_InitializesMapModule()
{
    Render<Map>();

    _module.VerifyInvoke("init");
}
```

- Výchozí `JSInterop.Mode` je **Strict** — nenastavené volání test shodí. Nech Strict, ať test odhalí nečekané JS volání.
- ✗ `JSRuntimeMode.Loose` plošně — schová chybějící setup i chybu v názvu funkce.

---

## Dispose a odběry

```csharp
[Fact]
public void Dispose_UnsubscribesFromNotifier()
{
    var notifier = new StockNotifier();
    Services.AddSingleton(notifier);
    var cut = Render<StockBadge>();

    DisposeComponents();

    notifier.SubscriberCount.Should().Be(0);   // prevence memory leaku okruhu
}
```

Pro komponenty s odběrem, timerem nebo `CancellationTokenSource` (viz `blazor/blazor-components.md` — Dispose) je tento test povinný.

---

## Co bUnit nepokryje

- Prerendering a `[PersistentState]` (dvojí inicializace) → integrační test přes `WebApplicationFactory` a kontrola HTML odpovědi.
- Reconnect, odpojení okruhu, chování přes reálný SignalR → E2E (Playwright).
- Vizuální vzhled a responzivita → E2E (`test-builders-e2e.md`).
