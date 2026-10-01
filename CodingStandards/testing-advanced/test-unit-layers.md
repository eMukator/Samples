# Testování — Unit testy po vrstvách

Co a jak testovat v každé vrstvě. Nástroje, pojmenování a test doubles viz `csharp/testing.md`.

## Unit vs. integrační test — rozhodnutí

| Testovaný kód | Typ testu | Proč |
|---------------|-----------|------|
| Entity, aggregate, value object, doménová služba | **Unit**, bez test doubles | Čistá logika bez I/O |
| Command handler | **Unit** s fake repository + fake UoW | Orchestrace: nenalezeno / uložení / Result |
| Query handler, repository, EF konfigurace | **Integrační** (Testcontainers) | Překlad LINQ → SQL ověří jen skutečná DB |
| Validator | **Unit** | Čistá pravidla |
| Decorator (validace, logování) | **Unit** s fake inner handlerem | Ověř, že při chybě inner neproběhne |
| Konzument zprávy, handler domain eventu | **Unit**; idempotence **integračně** | Inbox potřebuje DB s unique constraintem |
| Endpoint (HTTP mapování, status kódy, auth) | **Integrační** (WebApplicationFactory) | Routing, model binding, middleware |
| Blazor komponenta | **Unit** přes bUnit (viz `test-blazor.md`) | Renderování a interakce bez prohlížeče |
| Architektura (směr závislostí) | **Architektonický test** | Viz `clean-architecture/ca-enforcement.md` |

✗ Unit test query handleru s mockovaným `DbSet` nebo EF InMemory providerem — projde i dotaz, který v SQL selže.

---

## Struktura testovacích projektů

Testovací projekty zrcadlí `src/`:

| Struktura aplikace | Testovací projekty |
|--------------------|--------------------|
| Jeden projekt | `MyApp.UnitTests`, `MyApp.IntegrationTests` |
| Clean architecture | `MyApp.Domain.Tests`, `MyApp.Application.Tests`, `MyApp.IntegrationTests`, `MyApp.ArchitectureTests` |
| Modulární monolit | Totéž **per modul** + společný `MyApp.ArchitectureTests` |

Fake implementace (`FakeOrderRepository`, `FakeUnitOfWork`, `FakeEmailService`) jsou ve sdíleném projektu `MyApp.Tests.Common`, ne kopírované do každého testu.

---

## 1. Doména

Testuj **chování přes doménové metody**: invarianty, přechody stavů, vyvolané domain events.

```csharp
public sealed class OrderTests
{
    public sealed class Confirm
    {
        [Fact]
        public void WithLines_SetsConfirmedAndRaisesEvent()
        {
            var order = new OrderBuilder().WithLine(quantity: 2, price: 100m).Build();

            order.Confirm();

            order.Status.Should().Be(OrderStatus.Confirmed);
            order.DomainEvents.Should().ContainSingle()
                .Which.Should().BeOfType<OrderConfirmed>()
                .Which.Total.Amount.Should().Be(200m);
        }

        [Fact]
        public void WithoutLines_ThrowsDomainException()
        {
            var order = new OrderBuilder().Build();

            var act = () => order.Confirm();

            act.Should().Throw<DomainException>();
        }

        [Fact]
        public void WhenAlreadyConfirmed_ThrowsAndKeepsState()
        {
            var order = new OrderBuilder().WithLine().Confirmed().Build();
            order.ClearDomainEvents();

            var act = () => order.Confirm();

            act.Should().Throw<DomainException>();
            order.DomainEvents.Should().BeEmpty();   // neúspěšná operace nesmí vyvolat událost
        }
    }
}
```

### Value objects — hranice validace a rovnost

```csharp
public sealed class MoneyTests
{
    [Theory]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Ctor_WithNegativeAmount_Throws(decimal amount)
        => FluentActions.Invoking(() => new Money(amount, "CZK")).Should().Throw<DomainException>();

    [Theory]
    [InlineData("")]
    [InlineData("CZ")]
    [InlineData("CZKK")]
    public void Ctor_WithInvalidCurrency_Throws(string currency)
        => FluentActions.Invoking(() => new Money(10, currency)).Should().Throw<DomainException>();

    [Fact]
    public void Equals_SameAmountAndCurrency_AreEqual()
        => new Money(10, "czk").Should().Be(new Money(10, "CZK"));   // normalizace měny

    [Fact]
    public void Add_DifferentCurrencies_Throws()
        => FluentActions.Invoking(() => new Money(10, "CZK").Add(new Money(10, "EUR")))
            .Should().Throw<DomainException>();
}
```

Hranice testuj **těsně kolem** limitu (0, −0.01; 2, 3, 4 znaky) — ne náhodné hodnoty.

---

## 2. Command handlery

Ověřuj **orchestraci**, ne znovu doménová pravidla (ta jsou pokrytá v doménových testech).

```csharp
// MyApp.Tests.Common
public sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<OrderId, Order> _orders = [];

    public IReadOnlyCollection<Order> Added => _added;
    private readonly List<Order> _added = [];

    public FakeOrderRepository With(params Order[] orders)
    {
        foreach (var o in orders) _orders[o.Id] = o;
        return this;
    }

    public Task<Order?> GetAsync(OrderId id, CancellationToken ct) => Task.FromResult(_orders.GetValueOrDefault(id));

    public void Add(Order order) { _orders[order.Id] = order; _added.Add(order); }
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }
    public Task SaveChangesAsync(CancellationToken ct) { SaveCount++; return Task.CompletedTask; }
}
```

```csharp
public sealed class ConfirmOrderHandlerTests
{
    private readonly FakeOrderRepository _orders = new();
    private readonly FakeUnitOfWork _uow = new();
    private ConfirmOrderHandler CreateSut() => new(_orders, _uow);

    [Fact]
    public async Task HandleAsync_ExistingDraftOrder_ConfirmsAndSavesOnce()
    {
        var order = new OrderBuilder().WithLine().Build();
        _orders.With(order);

        var result = await CreateSut().HandleAsync(new ConfirmOrderCommand(order.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
        _uow.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_UnknownOrder_ReturnsNotFoundAndDoesNotSave()
    {
        var result = await CreateSut().HandleAsync(new ConfirmOrderCommand(OrderId.New()), CancellationToken.None);

        result.Error!.Code.Should().Be("not_found");
        _uow.SaveCount.Should().Be(0);
    }
}
```

Co v handler testu ověřit:
- [ ] Neexistující agregát → `Error.NotFound`, nic se neuloží
- [ ] Úspěch → `SaveChangesAsync` právě **jednou**
- [ ] Business chyba → `Result` s chybou, nic se neuloží
- [ ] Oprávnění k objektu (IDOR) → `NotFound` / `Forbidden`

⚠ Domain events dispatchuje EF interceptor — v unit testu handleru **neproběhnou**. Reakce na události testuj v testu handleru události, celý řetězec integračně.

---

## 3. Validátory

```csharp
public sealed class CreateOrderValidatorTests
{
    private readonly CreateOrderValidator _sut = new();

    [Fact]
    public void Validate_EmptyItems_HasError()
        => _sut.TestValidate(ValidCommand() with { Items = [] })
            .ShouldHaveValidationErrorFor(x => x.Items);

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
        => _sut.TestValidate(ValidCommand()).ShouldNotHaveAnyValidationErrors();

    private static CreateOrderCommand ValidCommand() => new(CustomerId: Guid.NewGuid(), Items: [new(ProductId: Guid.NewGuid(), Quantity: 1)]);
}
```

Vždy jeden **platný** základ (`ValidCommand()`) a v každém testu změna jedné vlastnosti přes `with` — test pak ověřuje právě to jedno pravidlo.

---

## 4. Decoratory

```csharp
[Fact]
public async Task HandleAsync_InvalidCommand_ThrowsAndDoesNotCallInner()
{
    var inner = new RecordingHandler<CreateOrderCommand, Result<OrderId>>();
    var validator = new InlineValidator<CreateOrderCommand>();
    validator.RuleFor(x => x.Items).NotEmpty();
    var sut = new ValidationDecorator<CreateOrderCommand, Result<OrderId>>(inner, [validator]);

    var act = () => sut.HandleAsync(new CreateOrderCommand(Guid.NewGuid(), []), CancellationToken.None);

    await act.Should().ThrowAsync<ValidationException>();
    inner.Calls.Should().Be(0);
}

// MyApp.Tests.Common — zaznamenává volání, vrací předem nastavený výsledek
public sealed class RecordingHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
{
    public int Calls { get; private set; }
    public TResult Result { get; set; } = default!;
    public Task<TResult> HandleAsync(TCommand command, CancellationToken ct) { Calls++; return Task.FromResult(Result); }
}
```

---

## 5. Determinismus

Test musí dát stejný výsledek při každém spuštění, v každém čase a pořadí.

| Zdroj nedeterminismu | Řešení |
|----------------------|--------|
| `DateTime.Now`, `DateTimeOffset.UtcNow` | `TimeProvider` → v testu `FakeTimeProvider` |
| Čas v doménové metodě | Předej ho parametrem: `order.Ship(trackingNumber, now)` — entita neinjektuje služby |
| `Guid.NewGuid()` / `OrderId.New()` | Neověřuj konkrétní hodnotu; potřebuješ-li ji, vytvoř agregát s ID z testu |
| `Random` | Injektuj `Random` se seedem, nebo předávej hodnotu |
| Pořadí v `Dictionary` / `HashSet` | Assert nezávislý na pořadí (`BeEquivalentTo`) nebo explicitní `OrderBy` |
| Kultura (`ToString("C")`, parsování) | `CultureInfo.InvariantCulture` v kódu, nebo nastav kulturu v testu |
| Sdílený statický stav mezi testy | Žádný — každý test si stav vytvoří sám |
| Bogus | Pevný seed: `Randomizer.Seed = new Random(42)` (v assembly fixture) |

```csharp
[Fact]
public void IsOverdue_AfterDueDate_ReturnsTrue()
{
    var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero));
    var invoice = new InvoiceBuilder().DueOn(new DateOnly(2026, 9, 30)).Build();

    invoice.IsOverdue(clock.GetUtcNow()).Should().BeTrue();
}
```

✗ `Task.Delay` / `Thread.Sleep` v testu kvůli čekání — použij `FakeTimeProvider.Advance()` nebo deterministické signály.

---

## 6. Jeden test = jedno chování

```csharp
// ✓ Více assertů, ale jedno chování („potvrzení objednávky")
order.Status.Should().Be(OrderStatus.Confirmed);
order.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<OrderConfirmed>();

// ✗ Dva scénáře v jednom testu — při selhání nevíš, který
order.Confirm();
order.Status.Should().Be(OrderStatus.Confirmed);
order.Ship(tracking, now);
order.Status.Should().Be(OrderStatus.Shipped);
```

- Název testu popisuje chování; když potřebuješ „And" v názvu, rozděl test.
- ✗ Logika v testu (`if`, `for`, výpočet očekávané hodnoty stejným algoritmem jako produkční kód). Očekávanou hodnotu napiš jako literál.

---

## 7. Mutační testy (Stryker.NET) — volitelně pro doménu

Coverage říká, který řádek se **spustil**, ne jestli by test **odhalil chybu**. Stryker záměrně mění kód (`>` → `>=`, `&&` → `||`) a ověřuje, že testy selžou.

```bash
dotnet tool install -g dotnet-stryker
cd tests/MyApp.Domain.Tests
dotnet stryker
```

```json
// tests/MyApp.Domain.Tests/stryker-config.json
{
  "stryker-config": {
    "project": "MyApp.Domain.csproj",
    "mutate": ["**/*.cs"],
    "thresholds": { "high": 80, "low": 60, "break": 50 }
  }
}
```

- Spouštěj jen nad **doménou** (core domain) — nad celou aplikací je pomalý a výsledky zašuměné.
- V CI noční / týdenní běh, ne v každém PR.
- Přeživší mutant = chybějící test hranice nebo zbytečný kód.
