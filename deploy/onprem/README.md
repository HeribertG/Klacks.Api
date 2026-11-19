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
- Docker Desktop (Windows) or Docker Engine + Compose plugin (Linux). macOS is not supported:
  `install.sh` needs Bash 4, macOS ships Bash 3.2.
- Outbound internet to `ghcr.io` and `github.com` (for images + auto-update)
- Open ports 80 + 443 (configurable)

## Install
**Windows (PowerShell):**
```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -ServerName klacks.example.com -Region de
```
**Linux:**
```bash
sudo SERVER_NAME=klacks.example.com REGION=de bash ./install.sh
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

Re-running the installer repairs an installation, it does not update it: it keeps the secrets,
the certificate and the version pinned in `.env`, then pulls and restarts that version. New
versions come through the auto-updater (Settings > Software Updates). On Linux, re-run it with
`sudo` as well. `.env` holds the database password, the JWT secret and the setup code, so
`install.sh` makes it readable by its owner only (mode 600; root when run with `sudo`), and after the first
automatic update the file belongs to root. Run later `docker compose` commands in this folder with `sudo` too,
because without it they cannot read `.env`.

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
bundle (and re-run the installer) before the switch has any effect. An installation whose
database still runs PostgreSQL 17 can take the current files as well, provided the installer runs
before the next `docker compose up -d`: it notices the PostgreSQL 17 data and keeps it on its own
engine (see "PostgreSQL major upgrade"). Without that run the database does not start; its data
stays intact.

Automatic updates are on for a new installation. The installer writes `UPDATE_AUTO_ENABLED=true`
and `UPDATE_NOTIFY_ONLY=false` into `.env`; they are applied once, on the first start, and only
if no update setting was saved yet. Afterwards switch updates off or to notify-only in the admin
UI (Settings > Software Updates); editing `.env` later has no effect on a saved choice.

## PostgreSQL major upgrade
A new installation runs PostgreSQL 19 (`ghcr.io/heribertg/klacks-postgres:19`) with its data in
the folder `pgdata` of the Docker volume `<project>_postgres_data`. Two optional `.env` keys
override this: `KLACKS_POSTGRES_IMAGE` (the image) and `KLACKS_POSTGRES_DATA_SUBDIR` (the folder in
the volume). A PostgreSQL version can never open the data of another major version; it refuses to
start instead, so a wrong value costs availability, not data.

**Existing PostgreSQL 17 data:** the installer (`install.sh`, `install.ps1`) reads the version of
the database already in the volume. If it is PostgreSQL 17 and `.env` names no image yet, it writes
`KLACKS_POSTGRES_IMAGE=pgvector/pgvector:pg17` and `KLACKS_POSTGRES_DATA_SUBDIR=pgdata`, so the
database keeps running unchanged. The installer never moves data to another version, and it leaves
an image already set in `.env` alone.

**The move to a new major version** is done by the auto-updater as a separate, signed engine
upgrade (never as part of a normal Klacks update). It needs the updater of Klacks 1.0.79 or newer;
an older installation gets it by re-running the installer, which pulls the current updater. This
version has no button for it in the admin UI yet: contact Klacks support to start it. The updater
then:

1. checks the signature, the current PostgreSQL version, the database roles and the free space;
2. stops `klacks-api` and writes a dump (`pg_dump -Fc`) of every database into the folder
   `engine-upgrade` of the volume;
3. stops the old database, pins the new image by its digest (`repo@sha256:...`) and the new folder
   `pg<major>` (e.g. `pg19`) in `.env`, starts the new database and restores the dumps into it;
4. compares row counts, extensions and indexes with the old database, starts `klacks-api` again and
   waits until it is healthy.

Any failure, also a restart of the updater halfway through, puts the old `.env` values back and
starts the old database again. The old folder (`pgdata`) is never changed or deleted.

**Before you start it:**
- Free disk space for Docker of at least 3 times the size of all databases, at least 2 GB.
- The databases may only be owned and used by the database user of the installation (`admin`).
  Other roles are not moved yet; the upgrade stops before changing anything if it finds one.
- Keep a current backup (`.env` and a `pg_dumpall`), as before every larger change.

**Manual rollback** (only needed when the automatic one could not finish): put the previous values
back into `.env` (`KLACKS_POSTGRES_IMAGE=pgvector/pgvector:pg17` and `KLACKS_POSTGRES_DATA_SUBDIR=pgdata`
for a PostgreSQL 17 installation), then run `sudo docker compose up -d postgres klacks-api` in this
folder. Everything entered in Klacks after the switch exists only in the new database and is lost
with a rollback.

After an upgrade the volume holds more than one database folder, and `.env` names the current one.
Back up `.env` again after the upgrade: an installer run on a `.env` without the two
`KLACKS_POSTGRES_*` lines, or with lines naming an older folder than the newest database in the
volume, stops instead of guessing which folder is current.

**Cleaning up:** the old folder and the dumps stay in the volume until you remove them. Once the new
version has run without problems for some days, take a fresh backup and delete them. `pgdata` is the
live database of every installation that was never upgraded, so the command below (Linux, project
`klacks`, former PostgreSQL 17 installation) only runs when `.env` names the new folder `pg19`:
```bash
sudo docker run --rm -v klacks_postgres_data:/pgvolume alpine:3.20 ls /pgvolume
sudo grep -qx 'KLACKS_POSTGRES_DATA_SUBDIR=pg19' .env \
  && sudo docker run --rm -v klacks_postgres_data:/pgvolume alpine:3.20 \
     sh -c 'test -f /pgvolume/pg19/PG_VERSION && rm -rf /pgvolume/pgdata /pgvolume/engine-upgrade'
```
After the cleanup a rollback to the old version is no longer possible.
