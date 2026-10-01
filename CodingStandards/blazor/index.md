# Blazor — Interactive Server

Primárně pro Blazor Web App (.NET 8+) s render mode **InteractiveServer**. Logika běží na serveru, UI se synchronizuje přes SignalR okruh (circuit). Navazuje na `csharp/`, `cqrs/`, `security/`, `tailwind/`.

## Moduly

@blazor-render-modes.md
@blazor-components.md
@blazor-data-state.md
@blazor-forms-security.md
@blazor-hosting.md

## Rychlá kontrola

- [ ] Render mode zvolen vědomě (globálně vs. per stránka) — veřejné obsahové stránky jako static SSR
- [ ] Prerendering: data se nenačítají dvakrát (`[PersistentState]` / `PersistentComponentState`), JS interop až v `OnAfterRenderAsync`
- [ ] Žádný `DbContext` injektovaný přímo do komponenty — `IDbContextFactory` nebo scope na operaci
- [ ] `HttpContext` / `IHttpContextAccessor` se v interaktivních komponentách nepoužívá — uživatel z `AuthenticationState`
- [ ] Autorizace v handleru / službě, ne jen schováním tlačítka (`AuthorizeView` je UX, ne bezpečnost)
- [ ] Komponenty s odběry, timery nebo `CancellationTokenSource` implementují `IAsyncDisposable`
- [ ] Aktualizace UI z jiného vlákna jen přes `InvokeAsync(StateHasChanged)`
- [ ] Data podle route parametru se načítají v `OnParametersSetAsync`, ne v `OnInitializedAsync`
- [ ] Neošetřená výjimka v event handleru shodí celý okruh → `ErrorBoundary` + ošetření očekávaných chyb
- [ ] Seznamy v cyklu mají `@key`; dlouhé seznamy `Virtualize` / `QuickGrid` se stránkováním
- [ ] Žádný `MarkupString` s uživatelským vstupem (XSS)
- [ ] Při více instancích: sticky sessions nebo Azure SignalR Service; vlastní reconnect UI
- [ ] Komponenty s logikou mají bUnit testy (viz `testing-advanced/test-blazor.md`)

## Kdy Interactive Server NEpoužívat

- Veřejný web s vysokou návštěvností a hlavně statickým obsahem → static SSR (+ enhanced navigation).
- Offline / PWA požadavek → WebAssembly (viz `responsive-pwa/`).
- Uživatelé na nestabilním připojení (mobilní terén) → každý klik je round-trip, odpojení = ztráta interaktivity.
