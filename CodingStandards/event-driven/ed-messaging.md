# Messaging — broker, chyby, sagy, event sourcing

## Volba transportu

| Transport | Kdy |
|-----------|-----|
| **Žádný broker** (domain events + outbox → in-process handler) | Modulární monolit, jedna DB |
| RabbitMQ | On-premise, klasické fronty a routing |
| Azure Service Bus | Azure, potřebuješ sessions (pořadí), scheduled messages, DLQ out-of-box |
| Kafka / Event Hubs | Vysoká propustnost, event streaming, replay historie |
| PostgreSQL / SQL Server transport (Wolverine, MassTransit) | Chceš messaging bez další infrastruktury |

Konkrétní broker schovej za knihovnu (Wolverine / MassTransit) nebo za úzký `IMessagePublisher` — business kód nezná SDK brokeru.

---

## Topologie a pojmenování

- **Event** → publish/subscribe (topic / exchange), libovolný počet odběratelů, publisher je nezná.
- **Command** → point-to-point (queue), právě jeden příjemce, odesílatel ho zná.
- Názvy: `{kontext}.{agregát}-{událost}.v{n}` → `ordering.order-confirmed.v1`.
- Každý konzument má **vlastní frontu** (subscription) — selhání jednoho neblokuje ostatní.

---

## Chyby, retry, DLQ

```csharp
// MassTransit — příklad konfigurace konzumenta
cfg.ReceiveEndpoint("billing.order-confirmed", e =>
{
    e.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1), TimeSpan.FromSeconds(2)));
    e.UseInMemoryOutbox(context);
    e.ConfigureConsumer<OrderConfirmedConsumer>(context);
});
```

| Typ chyby | Reakce |
|-----------|--------|
| Přechodná (timeout, deadlock, 503) | Retry s exponenciálním backoffem + jitter |
| Trvalá (nevalidní data, chybějící záznam, deserializace) | **Bez retry** → rovnou DLQ |
| Business odmítnutí (nedostatek zboží) | Není chyba — publikuj kompenzační událost (`OrderRejected`) |

- DLQ musí mít **alert** a postup pro ruční přehrání (redeliver) po opravě.
- Konzument nesmí zprávu „spolknout" (`catch {}` + ack) — ztráta dat.
- Poison message nesmí blokovat frontu — omez počet doručení (`MaxDeliveryCount`).

---

## Choreografie vs. orchestrace

| | Choreografie | Orchestrace (saga / process manager) |
|-|--------------|------------------|
| Princip | Služby reagují na události ostatních | Centrální koordinátor posílá commandy a čeká na odpovědi |
| Vhodné | 2–3 kroky, jednoduchý tok | 4+ kroků, kompenzace, timeouty |
| Riziko | Tok není nikde vidět, cyklické závislosti | Koordinátor jako „god object" |

### Saga — dlouhotrvající proces s kompenzací

```
OrderConfirmed → [Saga] → ReservePayment
PaymentReserved → [Saga] → ReserveStock
StockReservationFailed → [Saga] → CancelPayment (kompenzace) → OrderRejected
Timeout 15 min bez odpovědi → [Saga] → kompenzace
```

- Stav ságy persistuj (MassTransit `SagaStateMachine` + EF repository, Wolverine `Saga`).
- Každý krok musí mít **kompenzační akci** (distribuované transakce/2PC nepoužívej).
- Každý krok musí mít **timeout**.
- Kompenzace je business operace (storno, dobropis), ne technický rollback.

---

## Event Sourcing

Stav agregátu = posloupnost událostí, ne poslední snapshot v tabulce.

**Použij jen když:**
- audit a historie každé změny je business požadavek (finance, právní evidence),
- potřebuješ „temporal queries" (jak vypadal stav k datu X),
- doména je přirozeně událostní (ledger, workflow).

**Nepoužívej:** CRUD, reporting, když tým nemá zkušenost — výrazně zvyšuje složitost (projekce, verzování událostí, snapshoty, GDPR mazání).

Nástroje: **Marten** (PostgreSQL, MIT), KurrentDB (dříve EventStoreDB). Rozhodnutí zaznamenej jako ADR.

---

## Observability

- Propaguj `traceparent` (W3C) v hlavičkách zprávy — MassTransit i Wolverine to dělají s OpenTelemetry automaticky.
- Loguj `MessageId`, `Type`, `CorrelationId` jako structured properties (viz `observability/obs-logging.md`).
- Metriky: délka fronty, stáří nejstarší zprávy (consumer lag), počet zpráv v DLQ, nezpracované v outboxu.
- Health check `/ready` zahrnuje připojení k brokeru.

---

## Testování

- Handlery/konzumenty testuj **unit testem bez brokeru** — jsou to obyčejné třídy.
- Integrační test: Testcontainers s reálným brokerem (RabbitMQ / Service Bus emulator) — viz `testing-advanced/`.
- MassTransit `ITestHarness` / Wolverine `TrackActivity()` pro ověření, že byla zpráva publikována / zkonzumována.
- Vždy otestuj **duplicitní doručení** stejné zprávy (idempotence).
