---
name: deployment
description: "Verwende wenn an Deployment, Docker-Container, CI/CD, Nginx-Konfiguration oder Hetzner-Server gearbeitet wird"
---

# Deployment

## Server-Infrastruktur

| Komponente | Details |
|------------|---------|
| Server | Hetzner Cloud |
| IP | 157.180.42.127 |
| OS | Ubuntu 24.04 LTS |
| User | root |
| SSH Key | `C:\SourceCode\Hetzner\klacks_deployment_key` |

## URLs

| Service | URL | Port | Zugang |
|---------|-----|------|--------|
| Klacks UI | http://157.180.42.127:3000 | 3000 | via Nginx |
| Klacks UI (HTTPS) | https://157.180.42.127 | 443 | via Nginx |
| Klacks API | http://157.180.42.127:5000 | 5000 | via Nginx |
| Klacks API (HTTPS) | https://157.180.42.127:5443 | 5443 | via Nginx |
| Klacks Blazor (HTTPS) | https://157.180.42.127:7443 | 7443 | via Nginx |
| Klacks Marketplace (HTTPS) | https://157.180.42.127:7553 | 7553 | via Nginx |
| pgAdmin (HTTPS) | https://157.180.42.127:5553 | 5553 | via Nginx |
| PostgreSQL | nur Docker-intern | 5432 | nicht extern erreichbar |

## Docker Container

| Container | Image | Expose | Beschreibung |
|-----------|-------|--------|--------------|
| klacks-postgres | postgres:17-alpine | 5432 | PostgreSQL Datenbank |
| klacks-api | apps-klacks-api | 5000 | Backend API |
| klacks-ui | apps-klacks-ui | 80 | Angular Frontend |
| klacks-blazor | apps-klacks-blazor | 80 | Password Reset App |
| klacks-marketplace | klacks-marketplace | 80 | Blazor Server Marketplace App (Port 7003) |
| klacks-proxy | nginx:alpine | 80, 443, 3000, 5000, 5443, 7443, 5553, 7553 | Nginx Reverse Proxy |
| klacks-pgadmin | dpage/pgadmin4 | 80 | Datenbank-Verwaltung |

## CI/CD mit GitHub Actions

Push auf `main` → Automatisches Deployment

| Repository | Workflow | Dauer |
|------------|----------|-------|
| Klacks.Api | `deploy.yml` | ~2 Min |
| Klacks.Ui | `deploy.yml` | ~3 Min |
| Klacks.Blazor | `deploy.yml` | ~2 Min |

### GitHub Secrets

| Secret | Beschreibung |
|--------|--------------|
| `SSH_HOST` | Server IP: `157.180.42.127` |
| `SSH_USERNAME` | SSH User: `root` |
| `SSH_PRIVATE_KEY` | SSH Private Key Inhalt |

## Full Reset via GitHub Actions

1. https://github.com/HeribertG/Klacks.Api/actions/workflows/reset-and-deploy.yml
2. "Run workflow" klicken
3. `RESET` eingeben zur Bestätigung

## Manueller Reset via SSH

```bash
# SSH-Verbindung
ssh -i "C:\SourceCode\Hetzner\klacks_deployment_key" root@157.180.42.127

cd /root/apps

# Container stoppen
docker compose down

# DB-Volumes löschen
docker volume rm apps_postgres_data apps_pgadmin_data

# Repos aktualisieren
for repo in Klacks.Api Klacks.Ui Klacks.Blazor; do
  cd $repo && git fetch origin && git reset --hard origin/main && cd ..
done

# Neu bauen und starten
docker compose build --no-cache
docker compose up -d

# Logs prüfen
docker compose logs -f klacks-api
```

## Nginx Reverse Proxy

### Port 3000 (UI + API)
```
/api/*        → klacks-api:5000
/marketplace  → marketplace:80
/*            → klacks-ui:80
```

### Port 5000 (API Direct)
```
/*      → klacks-api:5000
```

## Konfiguration

### appsettings.Production.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=Klacks;Username=admin;Password=admin"
  },
  "Cors": {
    "Host": "http://157.180.42.127:3000"
  },
  "Database": {
    "InitializeOnStartup": true
  }
}
```

### environment.prod.ts (Frontend)

```typescript
export const environment = {
  production: true,
  baseUrl: '/api/backend/',
};
```

## Troubleshooting

```bash
# Container-Logs prüfen
docker compose logs klacks-api
docker compose logs klacks-ui
docker compose logs klacks-marketplace

# Container-Status
docker compose ps

# DB-Verbindung testen
docker exec klacks-postgres psql -U admin -d Klacks -c "SELECT 1"
```
