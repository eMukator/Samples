# Blazor — Formuláře & bezpečnost

## EditForm

```razor
<EditForm Model="_model" OnValidSubmit="SubmitAsync">
    <DataAnnotationsValidator />
    <ValidationSummary />

    <label for="email">E-mail</label>
    <InputText id="email" @bind-Value="_model.Email" autocomplete="email" />
    <ValidationMessage For="() => _model.Email" />

    <label for="qty">Množství</label>
    <InputNumber id="qty" @bind-Value="_model.Quantity" />
    <ValidationMessage For="() => _model.Quantity" />

    <button type="submit" disabled="@_isSubmitting">Odeslat</button>
</EditForm>

@code {
    private readonly OrderFormModel _model = new();
    private bool _isSubmitting;

    private async Task SubmitAsync()
    {
        _isSubmitting = true;
        try
        {
            var result = await Dispatcher.SendAsync<CreateOrderCommand, Result<OrderId>>(_model.ToCommand(), _cts.Token);
            if (result.IsSuccess) Navigation.NavigateTo($"/orders/{result.Value}");
            else _errorMessage = result.Error!.Message;      // business chyba → zobraz, nevyhazuj
        }
        finally { _isSubmitting = false; }
    }
}
```

- Form model (`OrderFormModel`) je **mutable třída pro binding** — oddělený od commandu (immutable record) i od entity.
- ✗ Binding přímo na doménovou entitu (`@bind-Value="order.Status"`) — obchází invarianty.
- `OnValidSubmit`, ne `OnSubmit` + ruční `Validate()`.
- Validace ve formuláři je UX; command validator v pipeline (viz `cqrs/cqrs-pipeline.md`) se spustí tak jako tak.
- FluentValidation v EditFormu: komunitní integrace (`Blazored.FluentValidation`) — nebo validuj v handleru a chyby vrať do `ValidationMessageStore`.
- Formulářové atributy (`autocomplete`, `inputmode`, `label for`) viz `responsive-pwa/forms.md`.

### Neuložené změny

```razor
@* _isDirty nastav v OnFieldChanged EditContextu, ConfirmLeave volá context.PreventNavigation() *@
<NavigationLock ConfirmExternalNavigation="_isDirty" OnBeforeInternalNavigation="ConfirmLeave" />
```

---

## Autorizace

```razor
@page "/admin/orders"
@attribute [Authorize(Policy = "Orders.Manage")]    @* ✓ stránka *@

<AuthorizeView Policy="Orders.Manage">              @* ✓ UX — schová tlačítko *@
    <button @onclick="DeleteAsync">Smazat</button>
</AuthorizeView>
```

```csharp
// ✓ VŽDY i v handleru / službě — UI není bezpečnostní hranice
public async Task<Result<OrderId>> HandleAsync(DeleteOrderCommand command, CancellationToken ct)
{
    var authResult = await authorization.AuthorizeAsync(currentUser.Principal, "Orders.Manage");
    if (!authResult.Succeeded) return Error.Forbidden();
    var order = await orders.GetAsync(command.OrderId, ct);
    if (order is null || !order.IsAccessibleBy(currentUser.Id)) return Error.NotFound("Objednávka");   // IDOR
    ...
}
```

- Route parametr / `[SupplyParameterFromQuery]` je **uživatelský vstup** — ověř, že uživatel smí daný objekt vidět (IDOR).
- Okruh drží `AuthenticationState` z doby připojení. Odebrání role / zablokování účtu se projeví až po novém okruhu → pro citlivé operace ověřuj aktuální stav v DB, nebo nastav revalidaci (`RevalidatingServerAuthenticationStateProvider`, šablona Identity ho obsahuje).

---

## XSS

```razor
@* ✓ Výchozí — Razor enkóduje *@
<p>@comment.Text</p>

@* ✗ Nikdy s uživatelským vstupem *@
<p>@((MarkupString)comment.Text)</p>

@* ✓ Pokud HTML opravdu potřebuješ — sanitizace allowlistem (HtmlSanitizer) *@
<p>@((MarkupString)sanitizer.Sanitize(comment.Html))</p>
```

---

## Ochrana okruhu a serveru

```csharp
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(o =>   // CircuitOptions
{
    o.DetailedErrors = builder.Environment.IsDevelopment();   // ✗ nikdy true v produkci
    o.DisconnectedCircuitMaxRetained = 100;
    o.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    o.JSInteropDefaultCallTimeout = TimeSpan.FromSeconds(30);
    o.MaxBufferedUnacknowledgedRenderBatches = 10;
})
.AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = 32 * 1024;   // výchozí; nezvyšuj plošně kvůli uploadu — použij InputFile se streamem
});
```

- Každý okruh spotřebovává paměť serveru — neukládej do komponent velké kolekce; stránkuj.
- Upload přes `InputFile` + `OpenReadStream(maxAllowedSize)`; validace souborů viz `security/sec-uploads-deps.md`.
- Rate limiting na drahé operace (export, vyhledávání) — v handleru/službě, middleware se na SignalR zprávy neaplikuje.
- `app.UseAntiforgery()` je povinné (formuláře ve static SSR i enhanced forms).
- CSP podle `security/sec-headers.md` funguje i pro Blazor Server: `connect-src 'self'` pokrývá WebSocket na stejný origin (moderní prohlížeče). ✗ Nepřidávej plošné `wss:` — povolí WebSocket na libovolný host.
