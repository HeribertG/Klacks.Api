# Klacks On-Prem (Docker) — Quickstart

Run Klacks on your own Windows or Linux machine with Docker. Updates apply automatically.

## Requirements
- **A host with at least 8 GB RAM and 4 vCPU.** Klacks runs its retrieval models
  (embedder + reranker) locally inside `klacks-api`; both sessions stay resident and
  need a 3584M container limit. On a smaller host, see `KLACKS_API_MEMORY_LIMIT` in
  `.env.example` — it must always be lowered together with
  `KLACKS_API_IDLE_UNLOAD_MINUTES`. Both installers check the memory Docker reports and
  stop a first install below 7 GiB unless both values are passed, e.g.
  `sudo KLACKS_API_MEMORY_LIMIT=2560M KLACKS_API_IDLE_UNLOAD_MINUTES=30 bash ./install.sh` or
  `.\install.ps1 -ApiMemoryLimit 2560M -IdleUnloadMinutes 30`; they write them into `.env`
  so the auto-updater keeps them. On Windows, Docker Desktop gives its VM half of the PC
  memory, so a 16 GB PC is recommended.
- Docker Desktop (Windows/Mac) or Docker Engine + Compose plugin (Linux)
- Outbound internet to `ghcr.io` and `github.com` (for images + auto-update)
- Open ports 80 + 443 (configurable)

## Install
**Windows (PowerShell):**
```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -ServerName klacks.example.com -Region de
```
**Linux:**
```bash
SERVER_NAME=klacks.example.com REGION=de ./install.sh
```

The installer generates secrets + a self-signed certificate, pins the latest released
version, starts the stack, and waits until it is healthy. On Windows, install into a folder
on a local drive (e.g. `C:\klacks`): the script maps that folder into the Docker Desktop VM
so the auto-updater can recreate containers with the right bind mounts. Installations made
with an older `install.ps1` must re-run the script once from a current bundle, otherwise
automatic updates break the API's bind mounts. `-Region`/`REGION` is optional
and pre-configures the country's locale, holidays, worktime limits, surcharges and
industry presets on first boot — see [`regions/README.md`](regions/README.md) for the
list of supported country codes.

## First login
`admin@test.com` / `P@ssw0rt1` — **change this password immediately** and set your
mail/SMTP settings in the admin UI.

## ERP order import drop point
Order XML files can be picked up automatically from a folder inside the `klacks-api`
container, without going through the UI. The folder is a named Docker volume
(`api-erp-import:/app/ErpImport`) so it survives container restarts and updates. Ask
Klacksy (`get_erp_drop_point_settings`) for the exact resolved path, or use the default:
```bash
docker exec klacks-api mkdir -p /app/ErpImport/erp/orders
docker cp order.xml klacks-api:/app/ErpImport/erp/orders/order.xml
```
The `mkdir -p` step is only needed once — the sub-folder does not exist until either this
command, an upload through the settings UI, or `check_erp_drop_point_folder_health`
creates it. The file is picked up at the next scheduled run (hourly by default; ask
Klacksy to trigger one immediately with `trigger_erp_import_run`) and ends up in the
`processed/` or `error/` sub-folder next to it.

## Geo services and privacy
Out of the box, maps, address geocoding and routing use public services: Nominatim
(geocoding), OSRM and OpenRouteService (routing, travel times) and the OpenStreetMap tile
servers (maps, route PDF). Client addresses, coordinates and map tile requests therefore
leave the installation - the API sends addresses and coordinates, the browser loads the
tiles. This is the default `GEO_NO_OUTBOUND_TRAFFIC=false`.

**Keep client addresses inside the installation** - set in `.env`:
```
GEO_NO_OUTBOUND_TRAFFIC=true
MAP_TILE_CSP_SOURCES=
```
Keep the `MAP_TILE_CSP_SOURCES` line with an empty value: the browser then may load map
tiles from no host at all. Deleting the line instead brings back the OpenStreetMap hosts.
Then apply it with `docker compose up -d` (or re-run the installer, which keeps both values).
The API logs one line per geo service at start-up, so `docker compose logs klacks-api`
shows which services are off.

Consequences with `GEO_NO_OUTBOUND_TRAFFIC=true` and no own instances:
- No maps (dashboard map, map in the route PDF), no address search or suggestions, no
  routing or travel times from a routing service (straight-line estimates instead), and no
  OpenStreetMap / Street View links.
- Coordinate gap: addresses created or imported while the switch is on get no coordinates,
  so they have no map marker and route planning only uses coordinates that already exist.
  Addresses stay saveable. After switching back, the API call (Admin role only)
  `POST api/backend/RouteOptimization/geocode-all` fills in the missing coordinates.
- Weather for the company location (Klacksy, greeting) is derived from the company address
  by geocoding and is therefore unavailable; weather for explicit coordinates still works.
- Only these geo services are covered. Other outbound services are separate and keep
  running: weather (Open-Meteo, receives coordinates), public holidays (Nager.Date),
  translations (DeepL, receives the texts to translate), web search for Klacksy (Serper or
  Tavily, receives search terms; the greeting searches for traffic and events in the company's city),
  image pulls and auto-updates (`ghcr.io`, `github.com`), the marketplace
  (`KLACKS_MARKETPLACE_URL`) and any external LLM provider configured for Klacksy.

**Own instances instead of switching off:** point `GEO_GEOCODING_BASE_URL` (Nominatim
compatible), `GEO_ROUTING_BASE_URL` (OSRM), `GEO_OPENROUTESERVICE_BASE_URL` and
`GEO_MAP_TILE_URL_TEMPLATE` (e.g. `https://tiles.example.local/{z}/{x}/{y}.png`, with
`GEO_MAP_TILE_ATTRIBUTION`) at your own servers. With `GEO_NO_OUTBOUND_TRAFFIC=true`, the
well-known public OpenStreetMap services (openstreetmap.org, project-osrm.org,
openrouteservice.org and a few public tile hosts) are refused even when entered here. This is
a deny list of known hosts, not an allow list: any other host you enter is used, so only enter
servers you operate. URLs with credentials (`user:password@host`) are rejected. The tile URL
is sent to every browser, so `GEO_MAP_TILE_URL_TEMPLATE` must not contain credentials or API
keys. An own tile server must also be listed in
`MAP_TILE_CSP_SOURCES` (e.g. `MAP_TILE_CSP_SOURCES=https://tiles.example.local`), otherwise
the browser blocks its tiles, and it must send CORS headers (`Access-Control-Allow-Origin`),
otherwise the route PDF export fails.

Installations made before these settings existed: the auto-updater only swaps the image
versions. Take `docker-compose.yml` and `nginx/nginx.onprem.conf.template` from a current
bundle (and re-run the installer) before the switch has any effect.

Automatic updates are on for a new installation. The installer writes `UPDATE_AUTO_ENABLED=true`
and `UPDATE_NOTIFY_ONLY=false` into `.env`; they are applied once, on the first start, and only
if no update setting was saved yet. Afterwards switch updates off or to notify-only in the admin
UI (Settings > Software Updates); editing `.env` later has no effect on a saved choice.

Full guide (update, backup/restore, rollback, BYO certificate): see
[`docs/onprem-docker-install.md`](../../../docs/onprem-docker-install.md).
