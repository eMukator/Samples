# Logování & Observability

## Moduly

@obs-logging.md
@obs-health-metrics.md

## Rychlá kontrola

- [ ] Serilog nastaven se strukturovaným výstupem (JSON v produkci)
- [ ] Placeholders v logu, nikdy string interpolace
- [ ] Žádná citlivá data v logech (hesla, tokeny, PII)
- [ ] CorrelationId middleware aktivní
- [ ] Health checks: /health/live a /health/ready
- [ ] Vlastní metriky pro klíčové business operace
- [ ] OpenTelemetry traces pro external volání
