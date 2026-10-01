# Testování — Pokročilé vzory

## Moduly

@test-unit-layers.md
@test-integration.md
@test-builders-e2e.md
@test-blazor.md

## Pyramida

70 % Unit → 25 % Integration → 5 % E2E

## Rychlá kontrola

- [ ] Testovací pojmenování: `{Metoda}_{Scénář}_{Výsledek}`
- [ ] AAA pattern (Arrange / Act / Assert)
- [ ] Builder pattern pro testovací data
- [ ] WebApplicationFactory pro integrační testy
- [ ] Testcontainers pro DB (ne InMemory)
- [ ] Fake implementace místo mocků (FakeEmailService, FakePaymentGateway, FakeTimeProvider); NSubstitute jen pro chybové stavy, které fake neumí — viz `csharp/testing.md`
- [ ] Unit vs. integrační test podle vrstvy (`test-unit-layers.md`) — query handlery a repository vždy integračně
- [ ] Doména testovaná přes doménové metody včetně vyvolaných domain events
- [ ] Testy deterministické: žádné `DateTime.Now`, `Guid` bez kontroly, `Random` bez seedu, `Task.Delay`
- [ ] Jeden test = jedno chování; žádná logika (`if`, `for`) v testu
- [ ] Blazor komponenty přes bUnit, JSInterop ve Strict módu, test odhlášení odběrů při Dispose
- [ ] Integration events mají snapshot test a test zpětné kompatibility (`event-driven/ed-events.md`)
- [ ] Coverage dle vrstev (Domain ≥ 90 %, Application ≥ 80 %); mutační testy domény volitelně (Stryker, nočně)
