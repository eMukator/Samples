# Environment Konfigurace — Docker & Deployment

## Dockerfile — produkční

```dockerfile
# Dockerfile
# Fáze 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Kopíruj jen csproj soubory pro cache layer
COPY ["src/MyApp.Web/MyApp.Web.csproj",             "src/MyApp.Web/"]
COPY ["src/MyApp.Application/MyApp.Application.csproj", "src/MyApp.Application/"]
COPY ["src/MyApp.Infrastructure/MyApp.Infrastructure.csproj", "src/MyApp.Infrastructure/"]
RUN dotnet restore "src/MyApp.Web/MyApp.Web.csproj"

# Kopíruj zbytek a build
COPY . .
WORKDIR "/src/src/MyApp.Web"
RUN dotnet publish "MyApp.Web.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# Fáze 2: Runtime — minimální image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Non-root user pro bezpečnost
RUN addgroup --system appgroup && adduser --system appuser --ingroup appgroup

# Kopíruj publish výstup
COPY --from=build /app/publish .

# Změň vlastníka souborů
RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "MyApp.Web.dll"]
```

## docker-compose — lokální vývoj

```yaml
# docker-compose.yml
services:
  web:
    build:
      context: .
      dockerfile: Dockerfile
    ports:
      - "7001:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    env_file:
      - .env.local   # gitignorovaný soubor se secrets
    depends_on:
      db:
        condition: service_healthy
      redis:
        condition: service_healthy
    volumes:
      - ./logs:/app/logs   # logy dostupné na hostu
    restart: unless-stopped

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "DevPassword123!"
      MSSQL_PID: Developer
    ports:
      - "1433:1433"
    volumes:
      - sqldata:/var/opt/mssql
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P DevPassword123! -Q "SELECT 1" -C
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 30s

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"
    volumes:
      - redisdata:/data
    healthcheck:
      test: redis-cli ping
      interval: 10s
      timeout: 5s
      retries: 5
    command: redis-server --appendonly yes

  mailhog:
    image: mailhog/mailhog:latest
    ports:
      - "1025:1025"   # SMTP
      - "8025:8025"   # Web UI
    # appsettings.Development.json: Email:SmtpHost=localhost, SmtpPort=1025

volumes:
  sqldata:
  redisdata:
```

## .dockerignore

```dockerignore
# Build výstupy
**/bin/
**/obj/
**/publish/

# IDE
.vs/
.vscode/
**/*.user

# Git
.git/
.gitignore

# Testy
**/TestResults/
**/coverage/

# Secrets
.env*
**/appsettings.Local.json

# Dokumentace
**/*.md
docs/

# Logs
logs/
*.log
```

## Azure App Service — deployment

```yaml
# .github/workflows/deploy-azure.yml
name: Deploy to Azure

on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production

    steps:
      - uses: actions/checkout@v4

      - name: Login to Azure
        uses: azure/login@v2
        with:
          creds: ${{ secrets.AZURE_CREDENTIALS }}

      - name: Build and push Docker image
        run: |
          az acr login --name myappregistry
          docker build -t myappregistry.azurecr.io/myapp:${{ github.sha }} .
          docker push myappregistry.azurecr.io/myapp:${{ github.sha }}

      - name: Deploy to App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name:    myapp-prod
          images:      myappregistry.azurecr.io/myapp:${{ github.sha }}

      - name: Wait for health check
        run: |
          for i in {1..30}; do
            STATUS=$(curl -s -o /dev/null -w "%{http_code}" https://myapp.azurewebsites.net/health/ready)
            if [ "$STATUS" = "200" ]; then
              echo "Health check passed"
              exit 0
            fi
            echo "Attempt $i: status $STATUS, waiting..."
            sleep 10
          done
          echo "Health check failed after 5 minutes"
          exit 1
```

## Kubernetes — základní manifesty

```yaml
# deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  replicas: 3
  selector:
    matchLabels:
      app: myapp
  template:
    spec:
      containers:
        - name: myapp
          image: myregistry.io/myapp:latest
          ports:
            - containerPort: 8080
          env:
            - name: ASPNETCORE_ENVIRONMENT
              value: Production
            - name: ConnectionStrings__Default
              valueFrom:
                secretKeyRef:
                  name: myapp-secrets
                  key: connection-string
          livenessProbe:
            httpGet:
              path: /health/live
              port: 8080
            initialDelaySeconds: 30
            periodSeconds: 10
          readinessProbe:
            httpGet:
              path: /health/ready
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 5
          resources:
            requests:
              memory: "256Mi"
              cpu: "100m"
            limits:
              memory: "512Mi"
              cpu: "500m"
```
