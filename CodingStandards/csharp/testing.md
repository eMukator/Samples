# C# — Testování

## Framework a knihovny

- **xUnit** — test framework (výchozí pro .NET projekty)
- **FluentAssertions 7.x** — čitelné assertions. Verze 8+ je komerční; pro nové projekty bez licence zůstaň na 7.x nebo použij **Shouldly** (MIT)
- **Fake implementace** — výchozí test double (viz níže)
- **NSubstitute** — mock knihovna jen pro výjimky z pravidla níže. ✗ Moq v nových projektech (incident se SponsorLink, 2023)
- **`Microsoft.Extensions.TimeProvider.Testing`** — `FakeTimeProvider` pro čas
- **Bogus** — generování testovacích dat
- **Testcontainers** — integrace s DB (Docker)

## Test doubles — co kdy použít

Jedno pravidlo pro celý projekt (stejné jako `testing-advanced/`):

| Závislost | Test double |
|-----------|-------------|
| Doménový model, value objects, doménové služby | **Žádný** — testuj reálné objekty |
| Databáze (repository, DbContext, query handlery) | **Testcontainers** — reálná DB, ne InMemory provider ani mock |
| Vlastní porty k externím systémům (e-mail, platby, SMS, HTTP API) | **Fake** — jednoduchá in-memory implementace, sdílená všemi testy |
| Repository v unit testu command handleru | **Fake** in-memory repository (`Dictionary` uvnitř) |
| Čas | `FakeTimeProvider` |
| Logger | `NullLogger<T>.Instance` (ověřování logů jen když je log požadavek — pak `FakeLogger<T>`) |
| Simulace chyby, kterou fake neumí (timeout, výjimka v N-tém volání), nebo ověření, že se něco **nevolalo** | **NSubstitute** — výjimka, ne výchozí volba |

Proč fake místo mocku: fake se píše jednou a chová se jako skutečná implementace; mock se nastavuje v každém testu znovu a testy pak ověřují implementaci místo chování (křehké při refaktoringu).

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
    _repository.Add(new Order { Id = orderId, CustomerName = "Jan Novák", Total = 1500m });

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
// FakeOrderRepository.cs — v testovacím projektu, sdílený všemi testy
// (implementuje celé rozhraní repository projektu; zde zkráceně)
public sealed class FakeOrderRepository : IOrderRepository
{
    private readonly Dictionary<int, Order> _orders = [];

    public void Add(Order order) => _orders[order.Id] = order;

    public Task<Order?> GetByIdAsync(int id, CancellationToken ct = default)
        => Task.FromResult(_orders.GetValueOrDefault(id));

    public Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Order>>([.. _orders.Values]);

    public Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        _orders[order.Id] = order;
        return Task.FromResult(order);
    }
}

public class OrderServiceTests
{
    private readonly FakeOrderRepository _repository = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_repository, NullLogger<OrderService>.Instance);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsOrderDto()
    {
        // Arrange
        var order = new Faker<Order>()
            .RuleFor(o => o.Id, 1)
            .RuleFor(o => o.CustomerName, f => f.Name.FullName())
            .Generate();
        _repository.Add(order);

        // Act
        var result = await _sut.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenRepositoryTimesOut_PropagatesException()
    {
        // ✓ Výjimka z pravidla — chybový stav, který fake neumí → NSubstitute
        var repository = Substitute.For<IOrderRepository>();
        repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException());
        var sut = new OrderService(repository, NullLogger<OrderService>.Instance);

        var act = () => sut.GetByIdAsync(1);

        await act.Should().ThrowAsync<TimeoutException>();
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

Cíle po vrstvách viz `testing-advanced/test-builders-e2e.md` (Domain ≥ 90 %, Application ≥ 80 %, celkem ≥ 75 %).
