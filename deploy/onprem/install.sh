#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Klacks On-Prem (Docker) bootstrap for Linux. Idempotent: first run generates secrets +
# a self-signed certificate and pins the released version; re-runs preserve them and just
# pull + (re)start the stack.
#
# Usage:  SERVER_NAME=klacks.example.com ./install.sh        (also reads HTTP_PORT / HTTPS_PORT,
#         GHCR_USER / GHCR_TOKEN while ghcr packages are private)
#         REGION=de ./install.sh                              (country/region setup, see regions/README.md;
#         must match a regions/<code>.json file; omit to skip region setup)
#         sudo KLACKS_API_MEMORY_LIMIT=2560M KLACKS_API_IDLE_UNLOAD_MINUTES=30 bash ./install.sh
#                                                             (host below ~8 GB RAM; both or neither, see .env.example)
#         sudo bash ./install.sh                              (without Docker: asks to install Docker Engine via
#                                                             get.docker.com; INSTALL_DOCKER=yes answers without a terminal)
set -euo pipefail
cd "$(dirname "$0")"

step() { printf '\033[36m==> %s\033[0m\n' "$1"; }
warn() { printf '\033[33m!!  %s\033[0m\n' "$1"; }

fail() { printf '\033[31mXX  %s\033[0m\n' "$1" >&2; exit 1; }

DOCKER_INSTALL_URL=https://docs.docker.com/engine/install/
COMPOSE_INSTALL_URL=https://docs.docker.com/compose/install/linux/
GET_DOCKER_URL=https://get.docker.com
DOCKER_SCRIPT_DISTROS="Ubuntu, Debian, Fedora, CentOS, RHEL"
BYTES_PER_GIB=1073741824
# The compose limits add up to 6.25 GiB (postgres 2 GiB, klacks-api 3.5 GiB, ui/updater/nginx
# 0.25 GiB each). An 8 GB host reports about 7.6 GiB to Docker, so 7 GiB is the floor that still
# leaves the host some headroom.
MIN_DOCKER_MEMORY_GIB=7
MEMORY_OVERRIDE_KEYS=(KLACKS_API_MEMORY_LIMIT KLACKS_API_IDLE_UNLOAD_MINUTES)
# Database engine: the compose defaults (no KLACKS_POSTGRES_* in .env) start PostgreSQL 19 on the subdirectory pgdata.
# A volume that still holds PostgreSQL 17 data is pinned to its own engine; only the updater's engine upgrade moves it.
DEFAULT_POSTGRES_MAJOR=19
DEFAULT_POSTGRES_IMAGE=ghcr.io/heribertg/klacks-postgres:19
DEFAULT_POSTGRES_DATA_SUBDIR=pgdata
LEGACY_POSTGRES_MAJOR=17
LEGACY_POSTGRES_IMAGE=pgvector/pgvector:pg17
POSTGRES_CONTAINER=klacks-postgres
POSTGRES_VOLUME_SUFFIX=_postgres_data
POSTGRES_VOLUME_MOUNT=/pgvolume
# Runs inside the probe container (sh); prints "<folder>/ <major>" for every cluster folder of the volume.
POSTGRES_CLUSTER_PROBE="for d in $POSTGRES_VOLUME_MOUNT/*/; do test -f \"\${d}PG_VERSION\" && echo \"\${d} \$(head -n 1 \"\${d}PG_VERSION\")\"; done; true"

# Writes the URL to the file ('-' = stdout); fresh desktop installs often ship wget but no curl.
download() {
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$1" -o "$2"
  elif command -v wget >/dev/null 2>&1; then
    wget -q -O "$2" "$1"
  else
    return 1
  fi
}

# Only offered when the docker binary is missing: on a host with the distribution's own docker
# package the convenience script would conflict with it. Needs root and an explicit yes (from the
# terminal, or INSTALL_DOCKER=yes when no terminal is attached); the default is no.
offer_docker_install() {
  local answer="" script
  [ "$(id -u)" -eq 0 ] \
    || fail "Docker is not installed. Re-run this script with sudo to let it install Docker, or install Docker Engine with the Compose plugin yourself: $DOCKER_INSTALL_URL"
  if [ "${INSTALL_DOCKER:-}" = yes ]; then
    answer=y
  elif (exec </dev/tty) 2>/dev/null; then
    printf 'Docker is not installed. Install Docker Engine now with Docker'\''s official script (%s, for %s)? [y/N] ' "$GET_DOCKER_URL" "$DOCKER_SCRIPT_DISTROS"
    read -r answer </dev/tty || answer=""
  fi
  case "$answer" in
    y|Y|yes|YES) ;;
    *) fail "Docker is not installed. Install Docker Engine with the Compose plugin first ($DOCKER_INSTALL_URL), or re-run and answer y." ;;
  esac

  step "Installing Docker Engine (this takes a few minutes)..."
  script="$(mktemp)"
  if ! download "$GET_DOCKER_URL" "$script"; then
    rm -f "$script"
    fail "Could not download $GET_DOCKER_URL (curl or wget and an internet connection are needed). Install Docker yourself: $DOCKER_INSTALL_URL"
  fi
  if ! sh "$script"; then
    rm -f "$script"
    fail "Docker's install script failed - it supports $DOCKER_SCRIPT_DISTROS. Install Docker Engine with the Compose plugin yourself: $DOCKER_INSTALL_URL"
  fi
  rm -f "$script"
  if command -v systemctl >/dev/null 2>&1; then
    systemctl enable --now docker >/dev/null 2>&1 || warn "Could not start the Docker service automatically."
  fi
}

check_docker() {
  local out
  if ! command -v docker >/dev/null 2>&1; then
    offer_docker_install
    command -v docker >/dev/null 2>&1 \
      || fail "Docker is still not available after the installation. Install Docker Engine with the Compose plugin yourself: $DOCKER_INSTALL_URL"
  fi
  if ! out="$(docker version --format '{{.Server.Version}}' 2>&1)"; then
    case "$out" in
      *"permission denied"*)
        fail "This user may not talk to the Docker daemon. Re-run with sudo, or add the user to the docker group (sudo usermod -aG docker \$USER) and log in again." ;;
      *)
        fail "The Docker daemon is not reachable. Start it (sudo systemctl start docker) and re-run this script. Docker said: $out" ;;
    esac
  fi
  docker compose version >/dev/null 2>&1 \
    || fail "The Docker Compose plugin ('docker compose') is missing. Install it: $COMPOSE_INSTALL_URL - the standalone docker-compose v1 is not supported."
}

# Below the floor a first install stops unless the API limit is lowered; a re-run only warns, so an
# existing small installation can still be repaired by re-running the script.
check_memory() {
  local total gib msg
  total="$(docker info --format '{{.MemTotal}}' 2>/dev/null || true)"
  if ! [[ "$total" =~ ^[0-9]+$ ]] || [ "$total" -eq 0 ]; then
    warn "Could not read the memory available to Docker; skipping the memory check."
    return
  fi
  if [ "$total" -ge $((MIN_DOCKER_MEMORY_GIB * BYTES_PER_GIB)) ]; then return; fi
  gib="$(awk -v b="$total" -v d="$BYTES_PER_GIB" 'BEGIN { printf "%.1f", b / d }')"
  if [ -n "${ENVMAP[KLACKS_API_MEMORY_LIMIT]:-}" ]; then
    warn "Docker sees ${gib} GiB of memory; running with the reduced API limit ${ENVMAP[KLACKS_API_MEMORY_LIMIT]}."
    return
  fi
  msg="Docker sees only ${gib} GiB of memory, Klacks needs at least ${MIN_DOCKER_MEMORY_GIB} GiB (an 8 GB host): the containers may use up to 6.25 GiB. On a smaller host lower the API limit together with the idle unload, e.g. sudo KLACKS_API_MEMORY_LIMIT=2560M KLACKS_API_IDLE_UNLOAD_MINUTES=30 bash ./install.sh (see .env.example)."
  if [ "$FIRST_RUN" = true ]; then fail "$msg"; fi
  warn "$msg"
}

# Database clusters in the postgres volume, one line "<folder> <major>" per top-level folder with a PG_VERSION file;
# prints nothing when the volume does not exist, returns 1 when Docker cannot read it. Read in a throw-away container
# (volume read-only, no network, sh as entrypoint), so no PostgreSQL process ever opens the data. Uses the image of the
# existing database container when there is one, so nothing has to be pulled.
list_volume_postgres_clusters() {
  local volume="$1" image output
  docker volume inspect "$volume" >/dev/null 2>&1 || return 0
  image="$(docker container inspect --format '{{.Image}}' "$POSTGRES_CONTAINER" 2>/dev/null)" || image=""
  [ -n "$image" ] || image="$DEFAULT_POSTGRES_IMAGE"
  output="$(docker run --rm --network none --security-opt no-new-privileges --entrypoint sh \
    -v "$volume:$POSTGRES_VOLUME_MOUNT:ro" "$image" -c "$POSTGRES_CLUSTER_PROBE" 2>/dev/null)" || return 1
  printf '%s\n' "$output" | sed -n "s#^$POSTGRES_VOLUME_MOUNT/\([A-Za-z0-9._-]*\)/ \([0-9][0-9]*\)\$#\1 \2#p"
}

# Keeps an existing database on the engine it was created with. Only acts while .env names no image (missing or
# empty, the two cases compose treats alike): an image already pinned there, by this script or by the updater's engine
# upgrade, is never changed. Moving to another major version is the updater's job (dump and restore), never this one's.
# A volume with more than one cluster has seen an engine upgrade; without the pin from .env it is unclear which cluster
# is current, so the script stops instead of guessing (it could otherwise revive the old one). With a pin it also stops
# when another folder holds a newer cluster (e.g. .env restored from a backup made before the upgrade); an older one
# next to the pinned folder is the normal state after an upgrade (kept for rollback).
pin_existing_postgres_engine() {
  local subdir volume clusters major others newer
  subdir="${ENVMAP[KLACKS_POSTGRES_DATA_SUBDIR]:-$DEFAULT_POSTGRES_DATA_SUBDIR}"
  [[ "$subdir" =~ ^[A-Za-z0-9._-]+$ ]] && [ "$subdir" != . ] && [ "$subdir" != .. ] \
    || fail "KLACKS_POSTGRES_DATA_SUBDIR in .env must be a plain folder name, found '$subdir'."
  volume="${ENVMAP[COMPOSE_PROJECT_NAME],,}${POSTGRES_VOLUME_SUFFIX}"
  if ! clusters="$(list_volume_postgres_clusters "$volume")"; then
    if [ -n "${ENVMAP[KLACKS_POSTGRES_IMAGE]:-}" ]; then
      warn "Could not read the databases in the Docker volume $volume; keeping the database engine pinned in .env unchecked."
      return 0
    fi
    fail "Could not read the databases in the Docker volume $volume. Nothing was changed; check Docker (with private ghcr.io packages run 'docker login ghcr.io' first) and re-run this script."
  fi
  major="$(printf '%s\n' "$clusters" | awk -v s="$subdir" '$1 == s { print $2 }')"
  others="$(printf '%s\n' "$clusters" | awk -v s="$subdir" 'NF && $1 != s { printf "%s%s (PostgreSQL %s)", sep, $1, $2; sep = ", " }')"
  if [ -n "${ENVMAP[KLACKS_POSTGRES_IMAGE]:-}" ]; then
    newer="$(printf '%s\n' "$clusters" | awk -v s="$subdir" -v m="${major:-0}" 'NF && $1 != s && $2 + 0 > m + 0 { printf "%s%s (PostgreSQL %s)", sep, $1, $2; sep = ", " }')"
    [ -z "$newer" ] \
      || fail "The Docker volume $volume holds a newer database than the folder $subdir named in .env: $newer. Nothing was changed. If .env comes from a backup made before a PostgreSQL major upgrade, set KLACKS_POSTGRES_IMAGE and KLACKS_POSTGRES_DATA_SUBDIR to the current values; if the newer folder is left over from a failed upgrade, contact Klacks support (see README.md, PostgreSQL major upgrade)."
    return 0
  fi
  [ -z "$others" ] \
    || fail "The Docker volume $volume holds another database besides $subdir: $others. A PostgreSQL major upgrade has run, but .env names no database image. Nothing was changed. Put KLACKS_POSTGRES_IMAGE and KLACKS_POSTGRES_DATA_SUBDIR back into .env (see README.md, PostgreSQL major upgrade) and re-run this script."
  case "$major" in
    ""|"$DEFAULT_POSTGRES_MAJOR") ;;
    "$LEGACY_POSTGRES_MAJOR")
      ENVMAP[KLACKS_POSTGRES_IMAGE]="$LEGACY_POSTGRES_IMAGE"
      ENVMAP[KLACKS_POSTGRES_DATA_SUBDIR]="$subdir"
      warn "The database in $volume runs PostgreSQL $LEGACY_POSTGRES_MAJOR; .env keeps it on $LEGACY_POSTGRES_IMAGE (see README.md, PostgreSQL major upgrade)." ;;
    *)
      fail "The database in $volume ($subdir) was created with PostgreSQL $major, which this bundle cannot run. Nothing was changed. Set KLACKS_POSTGRES_IMAGE in .env to an image of PostgreSQL $major, or contact Klacks support." ;;
  esac
}

step "Checking Docker..."
check_docker

# The updater recreates the containers with bind mounts into this folder, so it has to stay where it is.
case "$(pwd -P)" in
  */Downloads|*/Downloads/*|*/Download|*/Download/*|/tmp|/tmp/*)
    warn "Klacks is installed in $(pwd -P). Automatic updates keep using this folder - do not delete it. Better: move the folder to a permanent place (e.g. ~/klacks) and run the installer there." ;;
esac

ENV_FILE=.env
FIRST_RUN=true
declare -A ENVMAP
if [ -f "$ENV_FILE" ]; then
  FIRST_RUN=false
  step "Existing .env found — preserving secrets."
  while IFS='=' read -r k v || [ -n "$k" ]; do
    [[ "$k" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] && ENVMAP["$k"]="$v"
  done < "$ENV_FILE"
else
  step "First run — generating a fresh .env."
fi

# Memory overrides go into .env: the updater recreates the containers from .env and never sees this shell.
for key in "${MEMORY_OVERRIDE_KEYS[@]}"; do
  if [ -n "${!key:-}" ]; then ENVMAP[$key]="${!key}"; fi
done
HAS_MEMORY_LIMIT="${ENVMAP[KLACKS_API_MEMORY_LIMIT]:+yes}"
HAS_IDLE_UNLOAD="${ENVMAP[KLACKS_API_IDLE_UNLOAD_MINUTES]:+yes}"
if [ "$HAS_MEMORY_LIMIT" != "$HAS_IDLE_UNLOAD" ]; then
  fail "Set KLACKS_API_MEMORY_LIMIT and KLACKS_API_IDLE_UNLOAD_MINUTES together - a lowered limit without the idle unload gets the API killed (see .env.example)."
fi

step "Checking memory..."
check_memory

gen_secret() { head -c "${1:-48}" /dev/urandom | base64 | tr -d '\n'; }
# One-time setup code (12 characters, no look-alike characters, grouped by four). The first visitor of a fresh
# Klacks needs it once to create the own admin account; without it only the sign-in with the test account works.
gen_setup_code() {
  local raw
  raw="$(head -c 512 /dev/urandom | LC_ALL=C tr -dc 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789')"
  printf '%s-%s-%s' "${raw:0:4}" "${raw:4:4}" "${raw:8:4}"
}
set_if_empty() { local k="$1" v="$2"; [ -z "${ENVMAP[$k]:-}" ] && ENVMAP["$k"]="$v" || true; }
set_if_absent() { local k="$1" v="$2"; [ -z "${ENVMAP[$k]+x}" ] && ENVMAP["$k"]="$v" || true; }

set_if_empty COMPOSE_PROJECT_NAME klacks
step "Checking the database engine..."
pin_existing_postgres_engine
# Absolute host path of this directory; the updater mounts it at the same path and uses it as its
# compose project dir so relative bind mounts resolve identically inside and outside the container.
ENVMAP[KLACKS_INSTALL_DIR]="$(pwd -P)"
ENVMAP[KLACKS_UPDATER_PROJECT_DIR]="$(pwd -P)"
set_if_empty POSTGRES_PASSWORD "$(gen_secret 48)"
set_if_empty JWT_SECRET "$(gen_secret 64)"
set_if_empty KLACKS_SETUP_CODE "$(gen_setup_code)"

# Certificate that wraps the DataProtection key ring. The ring itself lives in the database, so a
# pg_dump keeps it current even though it rotates every 90 days; this certificate is what stops
# that dump from also containing the key which opens every stored secret. Generated once and kept
# in .env, which has to be backed up anyway for POSTGRES_PASSWORD - so it costs no extra chore.
gen_dataprotection_cert() {
  local dir pass
  dir="$(mktemp -d)"
  pass="$(gen_secret 24)"

  if command -v openssl >/dev/null 2>&1; then
    openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
      -keyout "$dir/dp.key" -out "$dir/dp.crt" -subj "/CN=Klacks DataProtection" >/dev/null 2>&1
    openssl pkcs12 -export -out "$dir/dp.pfx" -inkey "$dir/dp.key" -in "$dir/dp.crt" \
      -passout "pass:$pass" >/dev/null 2>&1
  else
    docker run --rm -v "$dir:/certs" alpine/openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
      -keyout /certs/dp.key -out /certs/dp.crt -subj "/CN=Klacks DataProtection" >/dev/null 2>&1
    docker run --rm -v "$dir:/certs" alpine/openssl pkcs12 -export -out /certs/dp.pfx \
      -inkey /certs/dp.key -in /certs/dp.crt -passout "pass:$pass" >/dev/null 2>&1
  fi

  if [ ! -s "$dir/dp.pfx" ]; then
    rm -rf "$dir"
    return 1
  fi

  ENVMAP[DATAPROTECTION_CERT_BASE64]="$(base64 -w0 "$dir/dp.pfx" 2>/dev/null || base64 "$dir/dp.pfx" | tr -d '\n')"
  ENVMAP[DATAPROTECTION_CERT_PASSWORD]="$pass"
  rm -rf "$dir"
  return 0
}

if [ -z "${ENVMAP[DATAPROTECTION_CERT_BASE64]:-}" ]; then
  step "Generating the DataProtection certificate..."
  if gen_dataprotection_cert; then
    warn "DataProtection certificate created. Keep .env safe - without it every stored password and API key is unrecoverable."
  else
    warn "Could not generate the DataProtection certificate; the key ring stays on the api-dataprotection volume, which must then be backed up continuously."
  fi
fi
set_if_empty KLACKS_UPDATER_TAG latest
set_if_empty UPDATE_MANIFEST_BASE_URL https://github.com/HeribertG/Klacks.Api/releases/latest/download
# Initial auto-update policy (on by default), seeded once into the database; afterwards the admin UI decides.
set_if_empty UPDATE_AUTO_ENABLED true
set_if_empty UPDATE_NOTIFY_ONLY false
# Geo services: traffic stays on by default, see README.md for the no-outbound mode. The CSP key is
# only filled when absent, because an empty value is a deliberate operator choice (no tile host).
set_if_empty GEO_NO_OUTBOUND_TRAFFIC false
set_if_absent MAP_TILE_CSP_SOURCES "https://tile.openstreetmap.org https://*.tile.openstreetmap.org"

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
  if VER="$(download "${ENVMAP[UPDATE_MANIFEST_BASE_URL]%/}/stable.json" - 2>/dev/null | grep -o '"latestVersion"[^,]*' | grep -o '[0-9][0-9.]*' | head -1)" && [ -n "$VER" ]; then
    ENVMAP[KLACKS_API_TAG]="$VER"; ENVMAP[KLACKS_UI_TAG]="$VER"
    step "Pinned api + ui to $VER."
  else
    warn "Could not fetch manifest. Falling back to :latest tags."
    set_if_empty KLACKS_API_TAG latest; set_if_empty KLACKS_UI_TAG latest
  fi
fi

# .env holds the database password, the JWT secret, the DataProtection certificate and the setup code: owner-only
# before any secret is written. Compose (run with sudo) and the updater container both read it as root.
: > "$ENV_FILE"
chmod 600 "$ENV_FILE"
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
  HTTPS_SUFFIX=""
  [ "${ENVMAP[HTTPS_PORT]}" != 443 ] && HTTPS_SUFFIX=":${ENVMAP[HTTPS_PORT]}"
  printf '\033[32mKlacks is up at https://%s%s\033[0m\n' "$SERVER_NAME" "$HTTPS_SUFFIX"
else
  warn "API did not report healthy yet. Check: sudo docker compose logs klacks-api"
fi
echo ""
printf '\033[32mYour setup code:  %s\033[0m\n' "${ENVMAP[KLACKS_SETUP_CODE]}"
echo "   You need it once, when you open Klacks for the first time: it lets you create your own admin account."
echo "   It stays in .env as KLACKS_SETUP_CODE."
echo ""
echo "If Klacks shows the sign-in page instead: admin@test.com  /  P@ssw0rt1 (Klacks then asks for your own admin account)."
warn "Set the mail/SMTP settings in the admin UI after the first login."
