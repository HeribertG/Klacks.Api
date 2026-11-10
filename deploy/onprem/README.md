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
At the end the installer prints **your setup code** (also kept in `.env` as `KLACKS_SETUP_CODE`).
Open Klacks in the browser: the first page creates your own admin account and asks for this
code once. Afterwards the built-in test account `admin@test.com` is switched off for good, and
Klacks asks for the industry. Then set your mail/SMTP settings in the admin UI.

Without a setup code (an installation made before this version, or a Docker Compose setup with
an empty `KLACKS_SETUP_CODE`) Klacks shows the sign-in page: sign in once with
`admin@test.com` / `P@ssw0rt1`, then Klacks asks for your own admin account.

## ERP order import drop point
Order XML files can be picked up automatically from a folder, without going through the UI.
The folder is a normal folder on the server: `erp-import` next to `docker-compose.yml`
(bound onto `/app/ErpImport` in the `klacks-api` container), so an ERP running on the
server writes straight into it and the files survive container restarts and updates.
The drop point itself is the sub-folder `erp-import/erp/orders`; Settings > Import (expert
mode) shows its full path with a copy button, Klacksy (`get_erp_drop_point_settings`)
names it as well. The file is picked up at the next scheduled run (hourly by default; ask
Klacksy to trigger one immediately with `trigger_erp_import_run`). The file stays in
the folder; its import state (waiting, importing, processed, failed) is tracked per file
and shown in Settings > Import.

**Another location:** set `KLACKS_ERP_IMPORT_HOST_DIR` in `.env` to an absolute Linux path
(e.g. `/srv/erp-import`) and `KLACKS_ERP_IMPORT_HOST_PATH` to the same value, so Settings >
Import shows the right path. On Windows keep the default: the auto-updater runs compose from
a Linux container and cannot resolve a Windows path.

**Write access for the ERP (Linux):** Docker creates the folder as `root` on the first start;
the API then hands the top folder to its container user `appuser` (non-recursive, mode 2770, no access for other host users); a folder you prepared yourself (other owner or group) is left untouched
and never changes the ownership of the files inside. Give the account the ERP export runs
under write access through a shared group, for example:
```bash
docker compose exec klacks-api id -u appuser      # numeric uid of appuser in the image
sudo mkdir -p erp-import/erp/orders
sudo chown -R <uid>:<erp-group> erp-import
sudo chmod -R g+rwX,o-rwx erp-import              # group-writable, closed to other users
sudo find erp-import -type d -exec chmod g+s {} +  # new folders keep the group
```
Without an ERP on the server, let the ERP push over HTTPS with an ERP import token (created
in Settings > Import) or upload the file there.

**Upgrading from a named volume:** installations set up before this change kept the folder in
the Docker volume `<project>_api-erp-import`. The compose file of a current bundle no longer
mounts it, so copy its contents into the new folder once, then remove the old volume:
```bash
docker volume ls --filter name=api-erp-import      # e.g. klacks_api-erp-import
docker compose stop klacks-api
docker run --rm -v klacks_api-erp-import:/from -v "$(pwd)/erp-import":/to alpine cp -a /from/. /to/
docker compose up -d klacks-api
docker volume rm klacks_api-erp-import             # only after Settings > Import shows the files
```
On Windows (PowerShell) use `-v "${PWD}\erp-import:/to"`. Skipping the copy loses no orders
already imported, but files still waiting in the old volume are not picked up.

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
  Addresses stay saveable. After switching back, the background address geocoding fills in the
  missing coordinates by itself (postcode/city position, about one request per two seconds; customer
  addresses always, employee addresses only with the setting `GEOCODING_EMPLOYEE_ADDRESSES_ENABLED=true`).
  The API call (Admin role only) `POST api/backend/RouteOptimization/geocode-all` does the same at once.
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
openrouteservice.org, heigit.org and a few public tile hosts) are refused even when entered here. This is
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
