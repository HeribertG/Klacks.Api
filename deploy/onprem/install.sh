#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli Private. All rights reserved.
#
# Klacks On-Prem (Docker) bootstrap for Linux. Idempotent: first run generates secrets +
# a self-signed certificate and pins the released version; re-runs preserve them and just
# pull + (re)start the stack.
#
# Usage:  SERVER_NAME=klacks.example.com ./install.sh        (also reads HTTP_PORT / HTTPS_PORT,
#         GHCR_USER / GHCR_TOKEN while ghcr packages are private)
#         REGION=de ./install.sh                              (country/region setup, see regions/README.md;
#         must match a regions/<code>.json file; omit to skip region setup)
set -euo pipefail
cd "$(dirname "$0")"

step() { printf '\033[36m==> %s\033[0m\n' "$1"; }
warn() { printf '\033[33m!!  %s\033[0m\n' "$1"; }

step "Checking Docker..."
docker version --format '{{.Server.Version}}' >/dev/null
docker compose version >/dev/null

ENV_FILE=.env
declare -A ENVMAP
if [ -f "$ENV_FILE" ]; then
  step "Existing .env found — preserving secrets."
  while IFS='=' read -r k v; do
    [[ "$k" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] && ENVMAP["$k"]="$v"
  done < "$ENV_FILE"
else
  step "First run — generating a fresh .env."
fi

gen_secret() { head -c "${1:-48}" /dev/urandom | base64 | tr -d '\n'; }
set_if_empty() { local k="$1" v="$2"; [ -z "${ENVMAP[$k]:-}" ] && ENVMAP["$k"]="$v" || true; }

set_if_empty COMPOSE_PROJECT_NAME klacks
set_if_empty POSTGRES_PASSWORD "$(gen_secret 48)"
set_if_empty JWT_SECRET "$(gen_secret 64)"
set_if_empty KLACKS_UPDATER_TAG latest
set_if_empty UPDATE_MANIFEST_BASE_URL https://github.com/HeribertG/Klacks.Api/releases/latest/download

[ -n "${SERVER_NAME:-}" ] && ENVMAP[SERVER_NAME]="$SERVER_NAME"
set_if_empty SERVER_NAME localhost
ENVMAP[HTTP_PORT]="${HTTP_PORT:-80}"
ENVMAP[HTTPS_PORT]="${HTTPS_PORT:-443}"
SERVER_NAME="${ENVMAP[SERVER_NAME]}"

if [ -n "${REGION:-}" ]; then
  REGION_LOWER="$(echo "$REGION" | tr '[:upper:]' '[:lower:]')"
  [ -f "regions/${REGION_LOWER}.json" ] || { warn "regions/${REGION_LOWER}.json not found — aborting."; exit 1; }
  ENVMAP[REGION_SETUP_FILE]="/app/regions/${REGION_LOWER}.json"
  step "Region setup: ${REGION_LOWER} (applied once on first boot)."
fi

# Vendor trust root (signature public key) — single-line with literal \n.
if [ -z "${ENVMAP[UPDATE_SIGNATURE_PUBLIC_KEY]:-}" ]; then
  if [ -f update-public-key.pem ]; then
    PEM="$(awk 'BEGIN{ORS="\\n"}{print}' update-public-key.pem | sed 's/\\n$//')"
    ENVMAP[UPDATE_SIGNATURE_PUBLIC_KEY]="\"$PEM\""
  else
    warn "update-public-key.pem missing — auto-update signature verification cannot run until UPDATE_SIGNATURE_PUBLIC_KEY is set."
  fi
fi

# Pin the released version from the signed manifest.
if [ -z "${ENVMAP[KLACKS_API_TAG]:-}" ] || [ "${ENVMAP[KLACKS_API_TAG]}" = latest ]; then
  step "Resolving latest released version from the manifest..."
  if VER="$(curl -fsSL "${ENVMAP[UPDATE_MANIFEST_BASE_URL]%/}/stable.json" 2>/dev/null | grep -o '"latestVersion"[^,]*' | grep -o '[0-9][0-9.]*' | head -1)" && [ -n "$VER" ]; then
    ENVMAP[KLACKS_API_TAG]="$VER"; ENVMAP[KLACKS_UI_TAG]="$VER"
    step "Pinned api + ui to $VER."
  else
    warn "Could not fetch manifest. Falling back to :latest tags."
    set_if_empty KLACKS_API_TAG latest; set_if_empty KLACKS_UI_TAG latest
  fi
fi

: > "$ENV_FILE"
for k in "${!ENVMAP[@]}"; do echo "$k=${ENVMAP[$k]}" >> "$ENV_FILE"; done
step ".env written."

# TLS certificate (self-signed if none provided).
mkdir -p nginx/certs
if [ -f nginx/certs/server.crt ] && [ -f nginx/certs/server.key ]; then
  step "Reusing existing certificate in nginx/certs."
else
  step "Generating a self-signed certificate for $SERVER_NAME..."
  if command -v openssl >/dev/null 2>&1; then
    openssl req -x509 -newkey rsa:2048 -nodes -days 825 \
      -keyout nginx/certs/server.key -out nginx/certs/server.crt -subj "/CN=$SERVER_NAME" >/dev/null 2>&1
  else
    docker run --rm -v "$(pwd)/nginx/certs:/certs" alpine/openssl req -x509 -newkey rsa:2048 -nodes -days 825 \
      -keyout /certs/server.key -out /certs/server.crt -subj "/CN=$SERVER_NAME" >/dev/null 2>&1
  fi
  warn "Self-signed certificate created. Browsers warn until you install a trusted (BYO) cert into nginx/certs."
fi

if [ -n "${GHCR_USER:-}" ] && [ -n "${GHCR_TOKEN:-}" ]; then
  step "Logging in to ghcr.io..."
  echo "$GHCR_TOKEN" | docker login ghcr.io -u "$GHCR_USER" --password-stdin >/dev/null
fi

step "Pulling images..."
docker compose pull
step "Starting the stack..."
docker compose up -d

step "Waiting for the API to become healthy (first run migrates + seeds the database)..."
HEALTHY=false
for i in $(seq 1 60); do
  sleep 10
  STATUS="$(docker inspect --format '{{.State.Health.Status}}' klacks-api 2>/dev/null || echo starting)"
  [ "$STATUS" = healthy ] && { HEALTHY=true; break; }
  echo "   ...still starting ($i/60, status: $STATUS)"
done

echo ""
if [ "$HEALTHY" = true ]; then
  printf '\033[32mKlacks is up at https://%s\033[0m\n' "$SERVER_NAME"
else
  warn "API did not report healthy yet. Check: docker compose logs klacks-api"
fi
echo ""
printf '\033[32mDefault login:  admin@test.com  /  P@ssw0rt1\033[0m\n'
warn "SECURITY: change this password immediately after first login, and update the mail/SMTP settings in the admin UI."
