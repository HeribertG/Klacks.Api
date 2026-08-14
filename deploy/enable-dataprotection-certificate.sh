#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli Private. All rights reserved.
#
# Moves the DataProtection key ring from the api-dataprotection volume into the database, wrapped by
# a generated certificate. Run on the server, in the directory holding docker-compose-server.yml and
# .env (on the Hetzner box: /root/apps).
#
# Why: the key ring rotates every 90 days, so a backup of the volume goes stale unnoticed and only
# fails when restoring. In the database every pg_dump carries the current ring. The certificate is
# what keeps that dump from also holding the key that opens every stored secret - it never rotates,
# so it is saved once, and .env already has to be kept safe for POSTGRES_PASSWORD.
#
# AFTER running this you MUST re-enter the stored secrets once in the settings UI: values encrypted
# with the old ring cannot be read by the new one. The script prints which ones are affected.
# The old key is deliberately NOT imported - importing it would place the plaintext key that opens
# every current secret into the very dump this is meant to protect.

set -euo pipefail

ENV_FILE="${ENV_FILE:-.env}"
COMPOSE_FILE="${COMPOSE_FILE:-docker-compose-server.yml}"
POSTGRES_CONTAINER="${POSTGRES_CONTAINER:-klacks-postgres}"
POSTGRES_DB="${POSTGRES_DB:-Klacks}"
POSTGRES_USER="${POSTGRES_USER:-admin}"
CERT_DAYS=3650

step() { printf '==> %s\n' "$1"; }
warn() { printf '!!  %s\n' "$1"; }
fail() { printf 'ERROR: %s\n' "$1" >&2; exit 1; }

[ -f "$ENV_FILE" ] || fail "$ENV_FILE not found. Run this in the directory that holds it."

if grep -q '^DATAPROTECTION_CERT_BASE64=.\+' "$ENV_FILE"; then
  warn "DATAPROTECTION_CERT_BASE64 is already set in $ENV_FILE - nothing to do."
  warn "Generating a new certificate would make every already-migrated secret unreadable."
  exit 0
fi

step "Listing the secrets that will need re-entering..."
if docker ps --format '{{.Names}}' | grep -qx "$POSTGRES_CONTAINER"; then
  docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -t -c \
    "SELECT '  - ' || type FROM settings WHERE value LIKE 'ENC:%' ORDER BY type;" 2>/dev/null \
    | grep -v '^\s*$' || echo "  (none)"
else
  warn "Container $POSTGRES_CONTAINER is not running - cannot list affected secrets."
fi

printf '\nThese values have to be typed in again after the switch. Continue? [y/N] '
read -r answer
case "$answer" in
  [yY]) ;;
  *) echo "Aborted, nothing changed."; exit 0 ;;
esac

step "Generating the certificate (valid ${CERT_DAYS} days)..."
WORK_DIR="$(mktemp -d)"
trap 'rm -rf "$WORK_DIR"' EXIT

CERT_PASSWORD="$(head -c 24 /dev/urandom | base64 | tr -d '\n')"

if command -v openssl >/dev/null 2>&1; then
  openssl req -x509 -newkey rsa:2048 -nodes -days "$CERT_DAYS" \
    -keyout "$WORK_DIR/dp.key" -out "$WORK_DIR/dp.crt" \
    -subj "/CN=Klacks DataProtection" >/dev/null 2>&1
  openssl pkcs12 -export -out "$WORK_DIR/dp.pfx" -inkey "$WORK_DIR/dp.key" -in "$WORK_DIR/dp.crt" \
    -passout "pass:$CERT_PASSWORD" >/dev/null 2>&1
else
  docker run --rm -v "$WORK_DIR:/certs" alpine/openssl req -x509 -newkey rsa:2048 -nodes \
    -days "$CERT_DAYS" -keyout /certs/dp.key -out /certs/dp.crt \
    -subj "/CN=Klacks DataProtection" >/dev/null 2>&1
  docker run --rm -v "$WORK_DIR:/certs" alpine/openssl pkcs12 -export -out /certs/dp.pfx \
    -inkey /certs/dp.key -in /certs/dp.crt -passout "pass:$CERT_PASSWORD" >/dev/null 2>&1
fi

[ -s "$WORK_DIR/dp.pfx" ] || fail "Certificate generation failed - $ENV_FILE is unchanged."

CERT_BASE64="$(base64 -w0 "$WORK_DIR/dp.pfx")"

step "Backing up $ENV_FILE..."
BACKUP_FILE="${ENV_FILE}.before-dataprotection"
cp "$ENV_FILE" "$BACKUP_FILE"
chmod 600 "$BACKUP_FILE"

step "Writing the certificate into $ENV_FILE..."
{
  printf '\n# DataProtection key ring: stored in the database, wrapped by this certificate.\n'
  printf '# LOSING THIS MEANS LOSING EVERY STORED PASSWORD AND API KEY. Back up %s offline.\n' "$ENV_FILE"
  printf 'DATAPROTECTION_CERT_BASE64=%s\n' "$CERT_BASE64"
  printf 'DATAPROTECTION_CERT_PASSWORD=%s\n' "$CERT_PASSWORD"
} >> "$ENV_FILE"
chmod 600 "$ENV_FILE"

step "Restarting klacks-api..."
docker compose -f "$COMPOSE_FILE" up -d klacks-api

step "Checking which key ring is in use..."
sleep 10
if docker compose -f "$COMPOSE_FILE" logs --tail 200 klacks-api 2>/dev/null \
     | grep -q "key ring is stored in the database"; then
  step "OK - the key ring is in the database and wrapped by the certificate."
else
  warn "Could not confirm it from the log. Check manually:"
  warn "  docker compose -f $COMPOSE_FILE logs klacks-api | grep -i dataprotection"
  warn "Expected: 'key ring is stored in the database and wrapped by certificate <thumbprint>'"
  warn "If it says 'No DataProtection certificate configured', the variables did not reach the container."
  warn "Roll back by restoring $BACKUP_FILE and restarting."
fi

cat <<'EOF'

Next, in this order:
  1. Back up .env offline (password manager or vault). Without it the stored secrets are lost.
  2. Re-enter the secrets listed above in the settings UI and save each one.
  3. Verify: send a test mail, run the IMAP test.
  4. Only when all of that works, remove the api-dataprotection volume from the compose file.

Keep the volume until step 3 passes - it is the way back.
EOF
