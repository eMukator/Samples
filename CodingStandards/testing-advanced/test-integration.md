# Testování — Integrační Testy & WebApplicationFactory

## Projekt struktura

```
tests/
  MyApp.UnitTests/
    Services/
      OrderServiceTests.cs
    Validators/
      CreateOrderRequestValidatorTests.cs
  MyApp.IntegrationTests/
    Api/
      OrdersApiTests.cs
    Repositories/
      OrderRepositoryTests.cs
    Infrastructure/
      CustomWebApplicationFactory.cs
      DatabaseFixture.cs
      TestAuthHandler.cs
  MyApp.E2ETests/
    Playwright/
      OrderCheckoutTests.cs
```

## CustomWebApplicationFactory

```csharp
// CustomWebApplicationFactory.cs
public class CustomWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Testcontainers — skutečný SQL Server v Dockeru
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    public new async Task DisposeAsync() => await _dbContainer.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Nahraď DB za testovací
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));

            // Nahraď external services fake implementacemi
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService, FakeEmailService>();

            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway, FakePaymentGateway>();

            // Testovací autentizace
            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
        });

        builder.UseEnvironment("Testing");
    }

    // Pomocná metoda — aplikuj migrace a seeduj data
    public async Task InitializeDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    // Vytvoř klienta s konkrétním uživatelem
    public HttpClient CreateAuthenticatedClient(string userId = "test-user", string role = "User")
    {
        return WithWebHostBuilder(b =>
            b.ConfigureTestServices(s =>
                s.Configure<TestAuthHandlerOptions>(o =>
                {
                    o.UserId = userId;
                    o.Role   = role;
                }))).CreateClient();
    }
}
```

## TestAuthHandler — autentizace v testech

```csharp
// TestAuthHandler.cs
public class TestAuthHandlerOptions : AuthenticationSchemeOptions
{
    public string UserId { get; set; } = "test-user-id";
    public string Role   { get; set; } = "User";
    public string Email  { get; set; } = "test@example.com";
}

public class TestAuthHandler(
    IOptionsMonitor<TestAuthHandlerOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthHandlerOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Options.UserId),
            new Claim(ClaimTypes.Name,  Options.Email),
            new Claim(ClaimTypes.Email, Options.Email),
            new Claim(ClaimTypes.Role,  Options.Role),
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

## Integrační testy — vzory

```csharp
// OrdersApiTests.cs
[Collection("Integration")]
public class OrdersApiTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private HttpClient _client = default!;

    public async Task InitializeAsync()
    {
        await factory.InitializeDatabaseAsync();
        _client = factory.CreateAuthenticatedClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateOrder_WithValidData_Returns201WithLocation()
    {
        // Arrange
        var request = new CreateOrderRequest(
            CustomerName: "Jan Novák",
            Email:        "jan@example.com",
            Items:        [new(ProductId: 1, Quantity: 2)]);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var created = await response.Content.ReadFromJsonAsync<OrderDto>();
        created.Should().NotBeNull();
        created!.CustomerName.Should().Be("Jan Novák");
        created.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyItems_Returns400WithProblemDetails()
    {
        var request = new CreateOrderRequest("Jan", "jan@test.com", []);

        var response = await _client.PostAsJsonAsync("/api/v1/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Status.Should().Be(400);
        problem.Title.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetOrders_WithoutAuth_Returns401()
    {
        var anonClient = factory.CreateClient();  // bez autentizace

        var response = await anonClient.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

// Collection fixture — sdílená DB napříč testy ve stejné kolekci
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory> { }
```

## FakeEmailService — test double

```csharp
// FakeEmailService.cs — zachytí odeslané emaily pro assertion
public class FakeEmailService : IEmailService
{
    private readonly ConcurrentBag<EmailMessage> _sent = new();

    public IReadOnlyList<EmailMessage> SentMessages => _sent.ToList();

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        _sent.Add(message);
        return Task.CompletedTask;
    }

    public void Clear() => _sent.Clear();
}

// Použití v testu
[Fact]
public async Task CreateOrder_SendsConfirmationEmail()
{
    var emailService = factory.Services.GetRequiredService<IEmailService>() as FakeEmailService;
    emailService!.Clear();

    await _client.PostAsJsonAsync("/api/v1/orders", validRequest);

    emailService.SentMessages.Should().HaveCount(1);
    emailService.SentMessages[0].To.Should().Be("jan@example.com");
    emailService.SentMessages[0].Subject.Should().Contain("Potvrzení objednávky");
}
```
