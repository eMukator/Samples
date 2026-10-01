# Event-Driven Development

Komunikace přes události: in-process (domain events), mezi moduly a službami (integration events přes broker). Navazuje na `ddd/`, `cqrs/`, `observability/`.

## Moduly

@ed-events.md
@ed-outbox-inbox.md
@ed-messaging.md

## Rychlá kontrola

- [ ] Události pojmenované v minulém čase (`OrderConfirmed`), commandy v rozkazovacím (`ConfirmOrder`)
- [ ] Domain events zůstávají uvnitř bounded contextu; ven jdou jen integration events z `*.Contracts`
- [ ] Integration event se nikdy nepublikuje přímo z handleru — vždy přes **Outbox** ve stejné transakci
- [ ] Každý konzument je **idempotentní** (Inbox / deduplikace podle `MessageId`) — doručení je at-least-once
- [ ] Konzument nespoléhá na pořadí zpráv
- [ ] Kontrakty událostí se mění jen aditivně; breaking change = nový typ (`…V2`) — hlídá snapshot test a test zpětné kompatibility
- [ ] Každá zpráva nese `MessageId`, `OccurredAt`, `CorrelationId` (propagace do logů a traces)
- [ ] Retry s backoffem + dead-letter queue; DLQ je monitorovaná a má alert
- [ ] Žádná citlivá data (PII, tajemství) v payloadu bez důvodu — stačí ID
- [ ] Event sourcing jen s vědomým rozhodnutím (ADR), ne jako výchozí volba

## Kdy event-driven NEpoužívat

- Volající potřebuje **okamžitou odpověď** (validace, výpočet ceny) → synchronní volání.
- Jedna aplikace, jedna DB, žádná integrace → domain events v transakci stačí, broker je zbytečný.
- Tým nemá monitoring a tracing — asynchronní systém bez observability je nedebugovatelný.
