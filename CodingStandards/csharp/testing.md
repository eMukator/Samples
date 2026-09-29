# C# — Testování

## Framework a knihovny

- **xUnit** — test framework (výchozí pro .NET projekty)
- **FluentAssertions** — čitelné assertions
- **Moq** nebo **NSubstitute** — mockování
- **Bogus** — generování testovacích dat
- **Testcontainers** — integrace s DB (Docker)

## Pojmenování testů

```
{TestovanáMetoda}_{Scénář}_{OčekávanýVýsledek}
```

```csharp
// ✓ Dobré pojmenování
public async Task GetByIdAsync_WithValidId_ReturnsOrder()
public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
public async Task CreateAsync_WithInvalidRequest_ThrowsValidationException()
public async Task CreateAsync_WhenRepositoryFails_LogsErrorAndRethrows()
```

## Struktura testu — AAA pattern

```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_ReturnsOrder()
{
    // Arrange
    var orderId = 42;
    var expectedOrder = new Order { Id = orderId, CustomerName = "Jan Novák", Total = 1500m };
    _repositoryMock.Setup(r => r.GetByIdAsync(orderId, default))
                   .ReturnsAsync(expectedOrder);

    // Act
    var result = await _sut.GetByIdAsync(orderId);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(orderId);
    result.CustomerName.Should().Be("Jan Novák");
    result.Total.Should().Be(1500m);
}
```

## Unit testy

```csharp
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<ILogger<OrderService>> _loggerMock = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_repositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrderDto()
    {
        // Arrange
        var order = new Faker<Order>()
            .RuleFor(o => o.Id, 1)
            .RuleFor(o => o.CustomerName, f => f.Name.FullName())
            .Generate();

        _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(order);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(999, default)).ReturnsAsync((Order?)null);

        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetByIdAsync_WithInvalidId_ThrowsArgumentException(int invalidId)
    {
        var act = () => _sut.GetByIdAsync(invalidId);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*id*");
    }
}
```

## Integrace s databází (Testcontainers)

```csharp
public class OrderRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private AppDbContext _context = null!;
    private OrderRepository _sut = null!;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .Options;

        _context = new AppDbContext(options);
        await _context.Database.MigrateAsync();

        _sut = new OrderRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_CreatesOrderInDatabase()
    {
        var order = new Order { CustomerName = "Test", Total = 100m };

        var created = await _sut.AddAsync(order);

        created.Id.Should().BeGreaterThan(0);
        var fromDb = await _sut.GetByIdAsync(created.Id);
        fromDb.Should().NotBeNull();
        fromDb!.CustomerName.Should().Be("Test");
    }
}
```

## Co testovat

| Typ | Co testovat |
|-----|------------|
| Services | Business logika, transformace dat, rozhodovací větve |
| Repositories | CRUD operace, filtrování, projekce (integrační testy) |
| Controllers | Status kódy, response format (controller testy nebo integrační) |
| Validators | Validační pravidla — platné i neplatné vstupy |
| Extensions | Transformační logika, helper metody |

## Co netestovat

- EF Core internals (třetí strana)
- Simple property gettery/settery bez logiky
- DI registraci (pokryta integračními testy)

## Coverage

- Minimální coverage pro business logiku (Services, Domain): **80 %**
- Repositories: pokryty integračními testy
- Controllers: základní happy path + error cases
