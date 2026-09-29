# Environment Konfigurace

## Moduly

@env-appsettings.md
@env-docker.md

## Rychlá kontrola

- [ ] Secrets NIKDY v appsettings.json ani v kódu
- [ ] User Secrets pro lokální vývoj
- [ ] Strongly-typed options s ValidateOnStart()
- [ ] .dockerignore obsahuje .env* a appsettings.Local.json
- [ ] Docker image běží jako non-root user
- [ ] HEALTHCHECK v Dockerfile
- [ ] Kubernetes readiness/liveness proby nasměrovány na /health/*
