# Klacks On-Prem (Docker) — Quickstart

Run Klacks on your own Windows or Linux machine with Docker. Updates apply automatically.

## Requirements
- Docker Desktop (Windows/Mac) or Docker Engine + Compose plugin (Linux)
- Outbound internet to `ghcr.io` and `github.com` (for images + auto-update)
- Open ports 80 + 443 (configurable)

## Install
**Windows (PowerShell):**
```powershell
powershell -ExecutionPolicy Bypass -File .\install.ps1 -ServerName klacks.example.com
```
**Linux:**
```bash
SERVER_NAME=klacks.example.com ./install.sh
```

The installer generates secrets + a self-signed certificate, pins the latest released
version, starts the stack, and waits until it is healthy.

## First login
`admin@test.com` / `P@ssw0rt1` — **change this password immediately** and set your
mail/SMTP settings in the admin UI.

Full guide (update, backup/restore, rollback, BYO certificate): see
[`docs/onprem-docker-install.md`](../../../docs/onprem-docker-install.md).
