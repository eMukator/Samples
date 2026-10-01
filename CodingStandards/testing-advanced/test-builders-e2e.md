# Testování — Builder Pattern, Konvence & E2E

## Builder Pattern pro testovací data

```csharp
// OrderBuilder.cs
public sealed class OrderBuilder
{
    private int          _id           = 1;
    private string       _customerName = "Test Zákazník";
    private string       _email        = "test@example.com";
    private OrderStatus  _status       = OrderStatus.Pending;
    private List<OrderItem> _items     = [new() { ProductId = 1, Quantity = 1, UnitPrice = 100m }];
    private DateTimeOffset _createdAt  = DateTimeOffset.UtcNow;

    public OrderBuilder WithId(int id)               { _id = id; return this; }
    public OrderBuilder WithCustomer(string name)    { _customerName = name; return this; }
    public OrderBuilder WithEmail(string email)      { _email = email; return this; }
    public OrderBuilder WithStatus(OrderStatus s)    { _status = s; return this; }
    public OrderBuilder WithItems(params OrderItem[] items) { _items = [..items]; return this; }
    public OrderBuilder CreatedAt(DateTimeOffset at) { _createdAt = at; return this; }

    // Semantic shortcuts — čitelné v testech
    public OrderBuilder AsPending()   => WithStatus(OrderStatus.Pending);
    public OrderBuilder AsConfirmed() => WithStatus(OrderStatus.Confirmed);
    public OrderBuilder AsShipped()   => WithStatus(OrderStatus.Shipped);
    public OrderBuilder AsCancelled() => WithStatus(OrderStatus.Cancelled);

    public OrderBuilder WithExpiredCreatedAt()
        => CreatedAt(DateTimeOffset.UtcNow.AddDays(-31));

    public Order Build() => new()
    {
        Id           = _id,
        CustomerName = _customerName,
        Email        = _email,
        Status       = _status,
        Items        = _items,
        Total        = _items.Sum(i => i.Quantity * i.UnitPrice),
        CreatedAt    = _createdAt,
        CreatedBy    = "test"
    };

    // Build a rovnou persist do DB
    public async Task<Order> PersistAsync(AppDbContext context)
    {
        var order = Build();
        context.Orders.Add(order);
        await context.SaveChangesAsync();
        return order;
    }
}

// ⚠ DDD agregáty (ddd/ddd-tactical.md) nemají public settery — builder pak NEnastavuje
//   vlastnosti, ale volá doménové metody, takže testovací data projdou invarianty:
//   public Order Build()
//   {
//       var order = Order.Create(_customerId);
//       foreach (var l in _lines) order.AddLine(l.ProductId, l.Quantity, l.Price);
//       if (_confirmed) order.Confirm();
//       return order;
//   }

// Použití — velmi čitelné
var pendingOrder   = new OrderBuilder().Build();
var confirmedOrder = new OrderBuilder().AsConfirmed().WithId(42).Build();
var expiredOrder   = new OrderBuilder().AsPending().WithExpiredCreatedAt().Build();
var bigOrder       = new OrderBuilder()
    .WithCustomer("VIP Zákazník")
    .WithItems(
        new() { ProductId = 1, Quantity = 5, UnitPrice = 1000m },
        new() { ProductId = 2, Quantity = 2, UnitPrice = 500m })
    .Build();
```

---

## Pojmenování testů — konvence

```csharp
// Formát: {TestovanáMetoda}_{Scénář}_{OčekávanýVýsledek}
public class OrderServiceTests
{
    public class GetByIdAsync  // nested class = skupina pro každou metodu
    {
        [Fact] public async Task WithValidId_ReturnsOrderDto() { }
        [Fact] public async Task WithNonExistentId_ReturnsNull() { }
        [Fact] public async Task WithDeletedOrder_ReturnsNull() { }
        [Fact] public async Task WhenUnauthorized_ThrowsForbiddenException() { }
    }

    public class CreateAsync
    {
        [Fact] public async Task WithValidRequest_CreatesAndReturnsDto() { }
        [Fact] public async Task WithInvalidEmail_ThrowsValidationException() { }
        [Fact] public async Task WithEmptyItems_ThrowsValidationException() { }
        [Fact] public async Task WhenDbFails_LogsErrorAndRethrows() { }
    }

    public class UpdateStatusAsync
    {
        [Fact] public async Task FromPendingToConfirmed_UpdatesAndSendsEmail() { }
        [Fact] public async Task FromCancelledToAny_ThrowsInvalidOperation() { }

        [Theory]
        [InlineData(OrderStatus.Pending,   OrderStatus.Confirmed)]
        [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
        [InlineData(OrderStatus.Shipped,   OrderStatus.Delivered)]
        public async Task ValidTransition_Succeeds(
            OrderStatus from, OrderStatus to) { }
    }
}
```

---

## Test kategorie a filtry

```csharp
// ✓ Kategorie pro selektivní spouštění
[Trait("Category", "Unit")]
public class OrderServiceTests { }

[Trait("Category", "Integration")]
public class OrdersApiTests { }

[Trait("Category", "Slow")]  // testy trvající déle
public class ReportGenerationTests { }
```

```bash
# Spuštění jen rychlých unit testů (v pre-commit hooku)
dotnet test --filter "Category=Unit"

# Všechny testy kromě pomalých
dotnet test --filter "Category!=Slow"

# Konkrétní třída
dotnet test --filter "FullyQualifiedName~OrderServiceTests"
```

---

## E2E testy — Playwright

```bash
dotnet add package Microsoft.Playwright.NUnit
# nebo
dotnet add package Microsoft.Playwright.MSTest
```

```csharp
// OrderCheckoutTests.cs
[Parallelizable(ParallelScope.Self)]
[TestFixture]
public class OrderCheckoutTests : PageTest
{
    [Test]
    public async Task CompletePurchase_HappyPath()
    {
        // Arrange
        await Page.GotoAsync("https://localhost:7001");

        // Act — login
        await Page.GetByLabel("E-mail").FillAsync("test@example.com");
        await Page.GetByLabel("Heslo").FillAsync("TestPassword123!");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Přihlásit" }).ClickAsync();

        // Přidej do košíku
        await Page.GotoAsync("/products");
        await Page.GetByTestId("product-1-add-to-cart").ClickAsync();

        // Pokladna
        await Page.GotoAsync("/checkout");
        await Page.GetByLabel("Jméno na kartě").FillAsync("Jan Novák");
        await Page.GetByLabel("Číslo karty").FillAsync("4111111111111111");
        await Page.GetByLabel("Platnost").FillAsync("12/27");
        await Page.GetByLabel("CVC").FillAsync("123");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Objednat" }).ClickAsync();

        // Assert
        await Expect(Page.GetByText("Objednávka byla přijata")).ToBeVisibleAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("/order-confirmation/\\d+"));
    }

    [Test]
    public async Task Checkout_MobileViewport_WorksCorrectly()
    {
        await Page.SetViewportSizeAsync(390, 844);  // iPhone 15
        await Page.GotoAsync("/checkout");

        // Mobilní menu
        await Page.GetByLabel("Otevřít menu").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Navigation)).ToBeVisibleAsync();
    }
}
```

---

## Coverage cíle

| Vrstva | Minimum | Cíl |
|--------|---------|-----|
| Domain (business logika) | 90 % | 95 % |
| Application (services) | 80 % | 90 % |
| Infrastructure (repositories) | 70 % | 80 % |
| Controllers (API) | 60 % | 75 % |
| Celkový projekt | 75 % | 85 % |

```xml
<!-- csproj — coverage threshold v CI -->
<!-- Použij Coverlet s threshold parametrem -->
```

```bash
# CI — selhej pokud coverage pod 75 %
dotnet test --collect:"XPlat Code Coverage"
dotnet tool run reportgenerator \
    -reports:"coverage/**/coverage.cobertura.xml" \
    -targetdir:"coverage/report" \
    -reporttypes:"Html;Cobertura" \
    -assemblyfilters:"+MyApp.*"
```
