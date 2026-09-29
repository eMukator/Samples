# Testování — Pokročilé vzory

## Moduly

@test-integration.md
@test-builders-e2e.md

## Pyramida

70 % Unit → 25 % Integration → 5 % E2E

## Rychlá kontrola

- [ ] Testovací pojmenování: `{Metoda}_{Scénář}_{Výsledek}`
- [ ] AAA pattern (Arrange / Act / Assert)
- [ ] Builder pattern pro testovací data
- [ ] WebApplicationFactory pro integrační testy
- [ ] Testcontainers pro DB (ne InMemory)
- [ ] FakeEmailService / FakePaymentGateway místo mockování
- [ ] Coverage business logiky ≥ 80 %
