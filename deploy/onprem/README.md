# Klacks On-Prem (Docker) — Quickstart

Run Klacks on your own Windows or Linux machine with Docker. Updates apply automatically.

## Requirements
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
version, starts the stack, and waits until it is healthy. `-Region`/`REGION` is optional
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

Full guide (update, backup/restore, rollback, BYO certificate): see
[`docs/onprem-docker-install.md`](../../../docs/onprem-docker-install.md).
