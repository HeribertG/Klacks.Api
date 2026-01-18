# Klacks Deployment Dokumentation

## Übersicht

Diese Dokumentation beschreibt das Deployment der Klacks-Anwendung auf dem Hetzner Server.

## Server-Infrastruktur

| Komponente | Details |
|------------|---------|
| **Server** | Hetzner Cloud |
| **IP** | 157.180.42.127 |
| **OS** | Ubuntu 24.04 LTS |
| **User** | root |
| **SSH Key** | `C:\SourceCode\Hetzner\klacks_deployment_key` |

## Anwendungs-URLs

| Service | URL | Port |
|---------|-----|------|
| Klacks UI | http://157.180.42.127:3000 | 3000 |
| Klacks API | http://157.180.42.127:5000 | 5000 |
| Klacks Blazor | http://157.180.42.127:7002 | 7002 |
| pgAdmin | http://157.180.42.127:5050 | 5050 |
| PostgreSQL | 157.180.42.127:5432 | 5432 |

## Docker Container

| Container | Image | Beschreibung |
|-----------|-------|--------------|
| klacks-postgres | postgres:17-alpine | PostgreSQL Datenbank |
| klacks-api | apps-klacks-api | .NET 10 Backend API |
| klacks-ui | apps-klacks-ui | Angular 21 Frontend |
| klacks-blazor | apps-klacks-blazor | .NET Blazor App |
| klacks-proxy | nginx:alpine | Reverse Proxy |
| klacks-pgadmin | dpage/pgadmin4 | Datenbank-Admin |

## CI/CD mit GitHub Actions

### Automatisches Deployment

Bei jedem Push auf den `main` Branch werden die entsprechenden Workflows ausgelöst:

| Repository | Workflow | Trigger | Dauer |
|------------|----------|---------|-------|
| Klacks.Api | `deploy.yml` | Push auf `main` | ~2 Min |
| Klacks.Ui | `deploy.yml` | Push auf `main` | ~3 Min |
| Klacks.Blazor | `deploy.yml` | Push auf `main` | ~2 Min |

### GitHub Secrets (erforderlich)

Folgende Secrets müssen in jedem Repository konfiguriert sein:

| Secret | Beschreibung |
|--------|--------------|
| `SSH_HOST` | Server IP: `157.180.42.127` |
| `SSH_USERNAME` | SSH User: `root` |
| `SSH_PRIVATE_KEY` | Inhalt des SSH Private Keys |

### Workflow-Ablauf

```
Push auf main
    ↓
GitHub Actions startet
    ↓
SSH-Verbindung zum Server
    ↓
Git Pull (neuester Code)
    ↓
Docker Build (--no-cache)
    ↓
Container Restart
    ↓
Health Check
```

## Klacks.Ui Build

Das Angular 21 Frontend verwendet `@angular/forms/signals` mit der `Field` Komponente. Dies erfordert:

- **Node.js 22** (LTS)
- **Angular CLI 21.1.0** (spezifische Version!)

### Dockerfile

```dockerfile
FROM node:22-alpine AS build
WORKDIR /app

# Angular CLI 21.1.0 ist erforderlich für @angular/forms/signals
RUN npm install -g @angular/cli@21.1.0

COPY package.json package-lock.json* ./
RUN npm install --legacy-peer-deps

COPY . .
ENV NODE_OPTIONS="--max-old-space-size=4096"
RUN ng build --configuration=production

FROM nginx:alpine
COPY --from=build /app/dist/klacks.ui/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
```

## Datenbank Reset & Full Redeploy

Wenn Migrationen zurückgesetzt werden (nur Init-Migration vorhanden), muss die Datenbank gelöscht und neu erstellt werden.

### Manueller Reset via GitHub Actions

1. Gehe zu: https://github.com/HeribertG/Klacks.Api/actions/workflows/reset-and-deploy.yml
2. Klicke auf **"Run workflow"**
3. Gib `RESET` ein zur Bestätigung
4. Optional: "Skip database reset" aktivieren (nur Apps neu deployen)

### Was passiert beim Reset:

1. ✅ Alle Container werden gestoppt
2. ✅ Datenbank-Volumes werden gelöscht
3. ✅ Alle Repositories werden aktualisiert
4. ✅ Alle Docker Images werden neu gebaut
5. ✅ Alle Container werden gestartet
6. ✅ API initialisiert DB und seedet Daten

### Manueller Reset via SSH

```bash
# SSH-Verbindung
ssh -i "C:\SourceCode\Hetzner\klacks_deployment_key" root@157.180.42.127

# Zum App-Verzeichnis
cd /root/apps

# Alle Container stoppen
docker compose down

# Datenbank-Volumes löschen
docker volume rm apps_postgres_data apps_pgadmin_data

# Alle Repos aktualisieren
for repo in Klacks.Api Klacks.Ui Klacks.Blazor; do
  cd $repo && git fetch origin && git reset --hard origin/main && cd ..
done

# Alle Images neu bauen
docker compose build --no-cache

# Alle Container starten
docker compose up -d

# Logs prüfen
docker compose logs -f klacks-api
```

## Nginx Reverse Proxy

Der nginx-proxy Container routet Anfragen basierend auf Port und Pfad:

### Port 3000 (UI + API)
```
/api/*  → klacks-api:5000
/*      → klacks-ui:80
```

### Port 5000 (API Direct)
```
/*      → klacks-api:5000
```

### Port 80 (HTTP)
```
/api/*  → klacks-api:5000
/*      → klacks-ui:80
```

## Konfigurationsdateien

### appsettings.Production.json (Klacks.Api)

Wichtige Einstellungen:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Database=Klacks;Username=admin;Password=admin"
  },
  "Cors": {
    "Host": "http://157.180.42.127:3000",
    "Home": "http://157.180.42.127:3000"
  },
  "Database": {
    "InitializeOnStartup": true
  }
}
```

### environment.prod.ts (Klacks.Ui)

```typescript
export const environment = {
  production: true,
  baseUrl: '/api/backend/',
  baseAssistantUrl: '/api/backend/assistant/',
};
```

## Troubleshooting

### Container startet nicht

```bash
# Logs prüfen
docker compose logs klacks-api
docker compose logs klacks-ui

# Container Status
docker compose ps
```

### 405 Method Not Allowed

- Prüfe nginx.conf: Hat der Port eine `/api/` Route?
- Prüfe CORS-Einstellungen in appsettings.Production.json

### Datenbank-Verbindungsfehler

```bash
# PostgreSQL Container prüfen
docker compose logs klacks-postgres

# Direkte Verbindung testen
docker exec klacks-postgres psql -U admin -d Klacks -c "SELECT 1"
```

### Build-Fehler bei Klacks.Ui

- Stelle sicher, dass `@angular/cli@21.1.0` installiert wird
- Prüfe package-lock.json auf Konflikte
- Verwende `--legacy-peer-deps` bei npm install

### GitHub Actions fehlgeschlagen

```bash
# Workflow-Logs anzeigen
gh run view <RUN_ID> --repo HeribertG/Klacks.Api --log

# Letzte Runs anzeigen
gh run list --repo HeribertG/Klacks.Api --limit 5
```

## Lokale Entwicklung vs. Production

| Aspekt | Lokal | Production |
|--------|-------|------------|
| API URL | http://localhost:5000 | http://157.180.42.127:5000 |
| UI URL | http://localhost:4200 | http://157.180.42.127:3000 |
| DB Host | localhost | postgres (Docker Network) |
| CORS | localhost:4200 | 157.180.42.127:3000 |

## Dateien im Repository

```
Klacks.Api/
├── .github/
│   └── workflows/
│       ├── deploy.yml              # Auto-Deploy bei Push
│       └── reset-and-deploy.yml    # Manueller Full Reset
├── nginx-proxy/
│   └── nginx.conf                  # Reverse Proxy Config
├── docker-compose.yml              # Alle Services
├── Dockerfile                      # API Container
├── appsettings.Production.json     # Production Settings
└── DEPLOYMENT.md                   # Diese Dokumentation
```

## Kontakt & Support

- **Repository Owner:** HeribertG
- **Server:** Hetzner Cloud (157.180.42.127)

---

*Zuletzt aktualisiert: Januar 2026*
