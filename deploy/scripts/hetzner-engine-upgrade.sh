#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Runbook for the PostgreSQL major upgrade of the Klacks production server (Hetzner, Docker compose project "apps"),
# run ON the server as root by .github/workflows/hetzner-engine-upgrade.yml through hetzner-engine-upgrade-remote.sh.
# The upgrade itself is done by the updater (operation type 4, see PLAN-b contract); this script prepares, starts, watches
# and verifies it. Modes:
#   preflight          checks plus backups (pg_dumpall gzip, copy of .env); changes nothing running (may pull the :19 image)
#   upgrade            preflight, suspend the nightly reset cron, enqueue the signed operation, wait, verify
#   rollback           manual return to the old engine (restore .env.pre-pg19, start the old cluster); only before cleanup
#   regenerate-golden  rebuild the playground golden dump from PostgreSQL 19 (after a successful upgrade)
#   cleanup            delete the old cluster, the dump exchange directory and the old image (>= 7 days after success)
# Secrets are never printed: the .env file is read per key, the signing public key and signatures are only piped.
#
# Usage: hetzner-engine-upgrade.sh --mode <mode> [--confirm <word>] [--from 17] [--to 19] [--compose <file>]
#          [--enqueue <path>] [--manifest <url|file>] [--run-id <id>] [--pre-release]
# --pre-release (preflight only) skips the checks that need the published release (manifest block, updater tag, capability),
# for the check before the tag. Test hooks, honoured only with HEU_TEST=1: HEU_NOW_UTC=HH:MM replaces the clock; HEU_WAIT_SECONDS,
# HEU_POLL_SECONDS and the HEU_*_WAIT_SECONDS variables shorten waits.
# Needs bash, docker (compose plugin), python3, openssl, gzip, curl, crontab, flock.

set -euo pipefail

if [ "${HEU_TEST:-}" != "1" ]; then
  unset HEU_NOW_UTC HEU_WAIT_SECONDS HEU_POLL_SECONDS HEU_HEALTH_WAIT_SECONDS HEU_PG_READY_WAIT_SECONDS HEU_GOLDEN_WAIT_SECONDS
fi

readonly BLOCK_START_MINUTES=$((1 * 60 + 15))
readonly BLOCK_END_MINUTES=$((4 * 60))
readonly UPGRADE_RESERVE_MINUTES=45
readonly MIN_FREE_GB=5
readonly CAPABILITY_MAX_AGE_MINUTES=30
readonly CAPABILITY_ID='postgres-engine-upgrade/1'
readonly UPGRADE_WAIT_SECONDS="${HEU_WAIT_SECONDS:-1800}"
readonly POLL_SECONDS="${HEU_POLL_SECONDS:-10}"
readonly HEALTH_WAIT_SECONDS="${HEU_HEALTH_WAIT_SECONDS:-600}"
readonly PG_READY_WAIT_SECONDS="${HEU_PG_READY_WAIT_SECONDS:-300}"
readonly GOLDEN_HEALTH_WAIT_SECONDS="${HEU_GOLDEN_WAIT_SECONDS:-2700}"
readonly CLEANUP_MIN_AGE_DAYS=7
readonly MIN_DUMP_BYTES=1024
readonly MIN_BACKUP_FREE_KB=$((1024 * 1024))
readonly OPERATION_TYPE_POSTGRES_UPGRADE=4
readonly STATUS_SUCCEEDED=2
readonly STATUS_FAILED=3
readonly STATUS_ROLLED_BACK=4
readonly STATUS_CANCELLED=5
readonly STATUS_ROLLBACK_FAILED=6
readonly CRON_MARKER='#PG19-SWITCH# '
readonly SIGNATURE_PATTERN='^[A-Za-z0-9+/]+={0,2}$'
readonly VERSION_PATTERN='^[0-9]+\.[0-9]+\.[0-9]+$'
readonly UPGRADE_MARKER_NAME='upgrade-succeeded'
readonly SECONDS_PER_DAY=86400
readonly -a PG_ENV_KEYS=(KLACKS_POSTGRES_IMAGE KLACKS_POSTGRES_DATA_SUBDIR)
readonly STATUS_ACTIVE_SQL='status IN (0, 1)'
readonly EXPECTED_AUTH_CODE=401
readonly MANIFEST_FETCH_SECONDS=60
readonly API_CURL_SECONDS=20
readonly API_HEALTH_CURL_SECONDS=10
readonly CRON_RESET_PATTERN='klacks-playground/reset.sh'
readonly DB_NAME_PATTERN='^[A-Za-z0-9_]+$'
readonly UUID_PATTERN='^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$'
readonly DEFAULT_MANIFEST_URL='https://github.com/HeribertG/Klacks.Api/releases/latest/download/stable.json'
readonly CONFIRM_UPGRADE='SWITCH-PG19'
readonly CONFIRM_ROLLBACK='ROLLBACK-PG17'
readonly CONFIRM_GOLDEN='REGEN-GOLDEN'
readonly CONFIRM_CLEANUP='DELETE-PG17-CLUSTER'

APPS_DIR="${APPS_DIR:-/root/apps}"
PROJECT="${COMPOSE_PROJECT:-apps}"
PG_CONTAINER="${PG_CONTAINER:-klacks-postgres}"
API_CONTAINER="${API_CONTAINER:-klacks-api}"
UPDATER_CONTAINER="${UPDATER_CONTAINER:-klacks-updater}"
PG_USER="${PG_USER:-admin}"
APP_DB="${APP_DB:-Klacks}"
BACKUP_DIR="${BACKUP_DIR:-/root/backups/pg19-switch}"
GOLDEN_DIR="${GOLDEN_DIR:-/root/backups/klacks-playground}"
RESET_SCRIPT="${RESET_SCRIPT:-/root/backups/klacks-playground/reset.sh}"
DEPLOY_LOCK="${DEPLOY_LOCK:-/var/lock/klacks-api-deploy.lock}"
DOCKER_ROOT="${DOCKER_ROOT:-/var/lib/docker}"
PGDATA_ROOT="${PGDATA_ROOT:-/var/lib/postgresql/data}"
OLD_IMAGE="${OLD_IMAGE:-pgvector/pgvector:pg17}"
ENV_FILE="$APPS_DIR/.env"

mode=""
confirm=""
from_major=17
to_major=19
compose_file="$APPS_DIR/docker-compose.yml"
enqueue_script=""
manifest_src="$DEFAULT_MANIFEST_URL"
run_id="manual-$(date -u +%Y%m%dT%H%M%SZ)"
pre_release=0

declare -a FAILS=()
declare -a WARNS=()
work="$(mktemp -d)"
cron_suspended=0
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"

log() { printf '[engine-upgrade] %s %s\n' "$(date -u +%H:%M:%SZ)" "$*"; }
die() { log "ERROR: $*"; exit 1; }
pass() { log "PASS $1${2:+: $2}"; }
fail() { log "FAIL $1${2:+: $2}"; FAILS+=("$1"); }
warn() { log "WARN $1${2:+: $2}"; WARNS+=("$1"); }
skip() { log "SKIP $1${2:+: $2}"; }

while [ $# -gt 0 ]; do
  case "$1" in
    --mode) mode="${2:?}"; shift 2 ;;
    --confirm) confirm="${2:?}"; shift 2 ;;
    --from) from_major="${2:?}"; shift 2 ;;
    --to) to_major="${2:?}"; shift 2 ;;
    --compose) compose_file="${2:?}"; shift 2 ;;
    --enqueue) enqueue_script="${2:?}"; shift 2 ;;
    --manifest) manifest_src="${2:?}"; shift 2 ;;
    --run-id) run_id="${2:?}"; shift 2 ;;
    --pre-release) pre_release=1; shift ;;
    *) die "Unknown argument '$1'" ;;
  esac
done
if [[ ! "$from_major" =~ ^[0-9]+$ ]] || [[ ! "$to_major" =~ ^[0-9]+$ ]]; then die "--from and --to must be numbers"; fi
[ "$to_major" -gt "$from_major" ] || die "--to must be greater than --from"
[[ "$run_id" =~ ^[A-Za-z0-9._-]+$ ]] || die "--run-id contains unexpected characters"

new_subdir="pg${to_major}"
engine_image="ghcr.io/heribertg/klacks-postgres:${to_major}"
snapshot_prefix="$BACKUP_DIR/rowcounts-$run_id"

restore_cron() {
  local current
  if [ "$cron_suspended" -eq 1 ]; then
    current="$(crontab -l 2> /dev/null || true)"
    if [ -z "$current" ]; then
      crontab "$BACKUP_DIR/crontab.before-$timestamp" || return 1
    else
      printf '%s\n' "$current" | sed "s|^${CRON_MARKER}||" | crontab - || return 1
    fi
    cron_suspended=0
    log "Reset cron restored."
  fi
}
on_exit() {
  local code=$?
  restore_cron || log "WARNING: could not restore the reset cron, check 'crontab -l' for lines starting with '${CRON_MARKER}'."
  rm -rf "$work"
  exit "$code"
}
trap on_exit EXIT
trap 'exit 143' TERM
trap 'exit 130' INT HUP

psql_db() { # db sql
  docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d "$1" -X -A -t -v ON_ERROR_STOP=1 -c "$2"
}

psql_q() { # db sql; empty output instead of an error, for checks
  psql_db "$1" "$2" 2> /dev/null | tr -d '\r' || true
}

env_value() {
  local line value
  line="$(grep -m1 -E "^[[:space:]]*$1=" "$ENV_FILE" 2> /dev/null || true)"
  value="${line#*=}"
  value="${value%$'\r'}"
  case "$value" in
    \"*) value="${value#\"}"; value="${value%%\"*}" ;;
    \'*) value="${value#\'}"; value="${value%%\'*}" ;;
  esac
  printf '%s' "$value"
}

now_minutes() {
  local h m
  if [ -n "${HEU_NOW_UTC:-}" ]; then h="${HEU_NOW_UTC%%:*}"; m="${HEU_NOW_UTC##*:}"; else h="$(date -u +%H)"; m="$(date -u +%M)"; fi
  echo $((10#$h * 60 + 10#$m))
}

window_blocked() { # reserve_minutes
  local now
  now="$(now_minutes)"
  [ "$now" -ge $((BLOCK_START_MINUTES - $1)) ] && [ "$now" -lt "$BLOCK_END_MINUTES" ]
}

container_running() { [ "$(docker inspect --format '{{.State.Running}}' "$1" 2> /dev/null || true)" = "true" ]; }

pg_major() {
  local num
  num="$(psql_db postgres 'SHOW server_version_num' 2> /dev/null | tr -d '[:space:]')" || return 1
  [[ "$num" =~ ^[0-9]+$ ]] || return 1
  echo $((num / 10000))
}

list_databases() {
  psql_db postgres "SELECT datname FROM pg_database WHERE NOT datistemplate AND datname <> 'postgres' ORDER BY 1"
}

api_exec_curl() { # path [curl args]
  local path="$1"
  shift
  docker exec "$API_CONTAINER" curl -sS --max-time "$API_CURL_SECONDS" "$@" "http://localhost:5000$path"
}

api_healthy() { docker exec "$API_CONTAINER" curl -fsS --max-time "$API_HEALTH_CURL_SECONDS" http://localhost:5000/health > /dev/null 2>&1; }

wait_until() { # seconds command...
  local limit="$1" waited=0
  shift
  while [ "$waited" -lt "$limit" ]; do
    if "$@"; then return 0; fi
    sleep "$POLL_SECONDS"
    waited=$((waited + POLL_SECONDS))
  done
  return 1
}

pg_ready() { docker exec "$PG_CONTAINER" pg_isready -U "$PG_USER" > /dev/null 2>&1; }

json_get() { # file key...
  python3 - "$@" <<'PY'
import json
import sys

node = json.load(open(sys.argv[1], encoding="utf-8"))
for key in sys.argv[2:]:
    if isinstance(node, dict) and key in node:
        node = node[key]
    else:
        node = ""
        break
print("".join(ch for ch in str(node) if ch.isprintable()))
PY
}

public_key_file() {
  local raw
  raw="$(env_value UPDATE_SIGNATURE_PUBLIC_KEY)"
  [ -n "$raw" ] || return 1
  printf '%b\n' "$raw" > "$work/public.pem"
  openssl pkey -pubin -in "$work/public.pem" -noout > /dev/null 2>&1
}

fetch_manifest() {
  if [ -f "$manifest_src" ]; then
    cp "$manifest_src" "$work/stable.json"
  else
    curl -fsSL --proto '=https' --proto-redir '=https' --max-time "$MANIFEST_FETCH_SECONDS" -o "$work/stable.json" "$manifest_src"
  fi
  python3 -c "import json,sys; json.load(open(sys.argv[1]))" "$work/stable.json"
}

image_index_digest() {
  local out
  if out="$(docker buildx imagetools inspect "$1" --format '{{.Manifest.Digest}}' 2> /dev/null)" && [[ "$out" =~ ^sha256:[0-9a-f]{64}$ ]]; then
    echo "$out"
    return 0
  fi
  docker pull -q "$1" > /dev/null 2>&1 || return 1
  docker image inspect "$1" --format '{{range .RepoDigests}}{{println .}}{{end}}' | sed -n 's/^[^@]*@\(sha256:[0-9a-f]\{64\}\)$/\1/p' | head -n1
}

snapshot_counts() { # db outfile
  psql_db "$1" "SELECT n.nspname || '.' || c.relname, (xpath('/row/c/text()', query_to_xml(format('select count(*) as c from %I.%I', n.nspname, c.relname), false, true, '')))[1]::text FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace WHERE c.relkind IN ('r', 'p') AND NOT c.relispartition AND n.nspname NOT IN ('pg_catalog', 'information_schema') AND n.nspname NOT LIKE 'pg_toast%' ORDER BY 1" > "$2"
}

take_snapshots() {
  local db
  mkdir -p "$BACKUP_DIR"
  chmod 700 "$BACKUP_DIR"
  : > "$snapshot_prefix.databases"
  while IFS= read -r db; do
    [ -n "$db" ] || continue
    if [[ ! "$db" =~ $DB_NAME_PATTERN ]]; then
      fail "snapshot" "database name '$db' has unexpected characters"
      continue
    fi
    if ! snapshot_counts "$db" "$snapshot_prefix.$db.tsv"; then
      fail "snapshot" "cannot read the row counts of $db"
      continue
    fi
    echo "$db" >> "$snapshot_prefix.databases"
    log "Snapshot $db: $(wc -l < "$snapshot_prefix.$db.tsv" | tr -d ' ') tables, $(awk -F'|' '{s+=$2} END {print s+0}' "$snapshot_prefix.$db.tsv") rows."
  done < <(list_databases)
}

compare_counts() { # before after; prints every table that lost rows or is missing, exit 1 on any
  awk -F'|' 'FILENAME == ARGV[1] {after[$1]=$2; next} { if (!($1 in after)) { print "missing table " $1; bad=1 } else if (after[$1]+0 < $2+0) { print "fewer rows in " $1 ": " $2 " -> " after[$1]; bad=1 } } END { exit bad ? 1 : 0 }' "$2" "$1"
}

take_backups() {
  local dump="$BACKUP_DIR/pre-switch-$timestamp.sql.gz" free_kb size
  mkdir -p "$BACKUP_DIR"
  chmod 700 "$BACKUP_DIR"
  free_kb="$(df -Pk "$BACKUP_DIR" | awk 'NR==2 {print $4}')"
  if [ "${free_kb:-0}" -le "$MIN_BACKUP_FREE_KB" ]; then
    fail "backup" "less than 1 GB free in $BACKUP_DIR"
    return 0
  fi
  (
    umask 077
    docker exec "$PG_CONTAINER" pg_dumpall -U "$PG_USER" | gzip > "$dump"
  ) || { fail "backup" "pg_dumpall failed"; return 0; }
  gzip -t "$dump" || { fail "backup" "$dump failed gzip -t"; return 0; }
  size="$(stat -c %s "$dump")"
  if [ "$size" -lt "$MIN_DUMP_BYTES" ]; then
    fail "backup" "$dump is implausibly small ($size bytes)"
    return 0
  fi
  cp -p "$ENV_FILE" "$BACKUP_DIR/env-pre-switch-$timestamp"
  chmod 600 "$BACKUP_DIR/env-pre-switch-$timestamp"
  pass "backup" "$dump ($size bytes, gzip -t ok), .env copy written"
}

check_window() { # reserve_minutes
  if window_blocked "$1"; then
    fail "time window" "now is inside the blocked window 01:15-04:00 UTC (reset cron 02:00, maintenance 03:30, dump 03:45) or closer than $1 min to its start"
  else
    pass "time window" "outside 01:15-04:00 UTC with $1 min reserve"
  fi
}

check_disk() {
  local free_gb
  free_gb="$(df -Pk "$DOCKER_ROOT" 2> /dev/null | awk 'NR==2 {printf "%d", $4/1024/1024}')"
  if [[ ! "$free_gb" =~ ^[0-9]+$ ]]; then
    fail "disk" "cannot read free space of $DOCKER_ROOT"
  elif [ "$free_gb" -ge "$MIN_FREE_GB" ]; then
    pass "disk" "${free_gb} GB free"
  else
    fail "disk" "${free_gb} GB free, need ${MIN_FREE_GB}"
  fi
}

check_database_state() {
  local major databases roles ext invalid active
  if ! container_running "$PG_CONTAINER"; then
    fail "postgres" "$PG_CONTAINER is not running"
    return 0
  fi
  major="$(pg_major || true)"
  if [ "$major" = "$from_major" ]; then
    pass "postgres" "server major $major"
  else
    fail "postgres" "server major is '${major:-unknown}', expected $from_major"
    return 0
  fi

  databases="$(psql_q postgres "SELECT datname FROM pg_database WHERE NOT datistemplate AND datname <> 'postgres' ORDER BY 1" | paste -sd, -)"
  if [[ ",$databases," == *",$APP_DB,"* ]]; then pass "databases" "$databases"; else fail "databases" "$APP_DB missing (found: $databases)"; fi

  roles="$(psql_q postgres "SELECT rolname FROM pg_roles WHERE rolname !~ '^pg_' ORDER BY 1" | paste -sd, -)"
  if [ "$roles" = "$PG_USER" ]; then pass "roles" "only $PG_USER"; else fail "roles" "found '$roles', only the bootstrap superuser '$PG_USER' is supported (v1 limit)"; fi

  ext="$(psql_q "$APP_DB" "SELECT extname || ' ' || extversion FROM pg_extension WHERE extname IN ('vector', 'pg_trgm') ORDER BY 1" | paste -sd, -)"
  if [[ "$ext" == *"pg_trgm"* ]] && [[ "$ext" == *"vector"* ]]; then pass "extensions" "$ext"; else fail "extensions" "need vector and pg_trgm in $APP_DB, found '$ext'"; fi

  invalid="$(psql_q "$APP_DB" "SELECT count(*) FROM pg_index WHERE NOT indisvalid" | tr -d '[:space:]')"
  if [ "$invalid" = "0" ]; then pass "indexes" "no invalid index in $APP_DB"; else fail "indexes" "'${invalid:-unreadable}' invalid index(es) in $APP_DB"; fi

  active="$(psql_q "$APP_DB" "SELECT count(*) FROM update_history WHERE $STATUS_ACTIVE_SQL" | tr -d '[:space:]')"
  if [ "$active" = "0" ]; then pass "active operation" "none"; else fail "active operation" "'${active:-unreadable}' pending/running update operation(s)"; fi
}

check_signing_key() {
  if [ -z "$(env_value UPDATE_SIGNATURE_PUBLIC_KEY)" ]; then
    fail "signing key" "UPDATE_SIGNATURE_PUBLIC_KEY is empty in $ENV_FILE (engine upgrades need a signature)"
  elif public_key_file; then
    pass "signing key" "UPDATE_SIGNATURE_PUBLIC_KEY is set and parses as a public key"
  else
    fail "signing key" "UPDATE_SIGNATURE_PUBLIC_KEY does not parse as a PEM public key"
  fi
}

check_compose() {
  local new current
  if [ ! -f "$compose_file" ]; then
    fail "compose" "$compose_file not found"
    return 0
  fi
  if grep -q 'KLACKS_POSTGRES_IMAGE' "$compose_file" && grep -q 'KLACKS_POSTGRES_DATA_SUBDIR' "$compose_file"; then
    pass "compose parametrised" "KLACKS_POSTGRES_IMAGE and KLACKS_POSTGRES_DATA_SUBDIR present"
  else
    fail "compose parametrised" "$compose_file lacks KLACKS_POSTGRES_IMAGE / KLACKS_POSTGRES_DATA_SUBDIR"
  fi
  new="$(docker compose --project-name "$PROJECT" --project-directory "$APPS_DIR" --env-file "$ENV_FILE" -f "$compose_file" config --hash postgres 2> /dev/null | awk '$1 == "postgres" {print $2}')"
  current="$(docker inspect "$PG_CONTAINER" --format '{{ index .Config.Labels "com.docker.compose.config-hash" }}' 2> /dev/null || true)"
  if [ -z "$new" ] || [ -z "$current" ]; then
    fail "compose hash" "cannot compute (new='${new:-}', running='${current:-}')"
  elif [ "$new" = "$current" ]; then
    pass "compose hash" "the postgres service config hash is unchanged, compose will not recreate the container"
  else
    fail "compose hash" "config hash of the new compose differs from the running container (new $new, running $current): the next compose up would recreate postgres"
  fi
}

check_manifest() {
  local latest m_major image sha sig live
  if ! fetch_manifest 2> /dev/null; then
    fail "manifest" "cannot fetch stable.json from $manifest_src"
    return 0
  fi
  latest="$(json_get "$work/stable.json" latestVersion)"
  m_major="$(json_get "$work/stable.json" artifacts postgres major)"
  image="$(json_get "$work/stable.json" artifacts postgres docker image)"
  sha="$(json_get "$work/stable.json" artifacts postgres docker sha256)"
  sig="$(json_get "$work/stable.json" artifacts postgres docker signature)"
  if [[ ! "$latest" =~ $VERSION_PATTERN ]]; then
    fail "manifest" "latestVersion of stable.json is not X.Y.Z"
    return 0
  fi
  echo "$latest" > "$work/latest.version"
  if [ "$m_major" != "$to_major" ] || [ "$image" != "$engine_image" ] || [[ ! "$sha" =~ ^sha256:[0-9a-f]{64}$ ]] || [[ ! "$sig" =~ $SIGNATURE_PATTERN ]]; then
    fail "manifest" "stable.json $latest has no usable artifacts.postgres.docker for major $to_major"
    return 0
  fi
  if ! public_key_file; then
    fail "manifest" "no usable public key to verify the signature"
    return 0
  fi
  printf 'klacks-postgres-engine\n%s\n%s\n%s' "$to_major" "$image" "$sha" > "$work/engine.payload"
  if ! printf '%s' "$sig" | base64 -d > "$work/engine.sig" 2> /dev/null; then
    fail "manifest" "the engine signature in stable.json is not valid Base64"
    return 0
  fi
  if ! openssl dgst -sha256 -verify "$work/public.pem" -signature "$work/engine.sig" "$work/engine.payload" > /dev/null 2>&1; then
    fail "manifest" "engine signature in stable.json $latest does not verify with the server's public key"
    return 0
  fi
  live="$(image_index_digest "$image" || true)"
  if [ "$live" = "$sha" ]; then
    pass "manifest" "stable.json $latest: signed digest equals the current $image ($sha)"
  else
    fail "manifest" "signed digest $sha differs from the current $image digest '${live:-unreadable}' (image rebuilt after the release?)"
  fi
  printf '%s\n%s\n%s\n' "$image" "$sha" "$sig" > "$work/engine.values"
}

check_updater_and_capability() {
  local latest updater_image updater_tag cap
  latest="$(cat "$work/latest.version" 2> /dev/null || true)"
  updater_image="$(docker inspect "$UPDATER_CONTAINER" --format '{{.Config.Image}}' 2> /dev/null || true)"
  updater_tag="${updater_image##*:}"
  if [ -n "$latest" ] && [ "$updater_tag" = "$latest" ]; then
    pass "updater tag" "$updater_image equals the release $latest"
  else
    fail "updater tag" "updater image '${updater_image:-none}' does not carry the release tag '${latest:-unknown}'"
  fi
  cap="$(psql_q "$APP_DB" "SELECT (value::jsonb -> 'capabilities') ? '$CAPABILITY_ID' AND (value::jsonb ->> 'reportedAtUtc')::timestamptz > now() - interval '$CAPABILITY_MAX_AGE_MINUTES minutes' FROM settings WHERE type = 'UPDATER_CAPABILITIES' LIMIT 1" | tr -d '[:space:]')"
  if [ "$cap" = "t" ]; then
    pass "capability" "updater reported $CAPABILITY_ID within $CAPABILITY_MAX_AGE_MINUTES min"
  elif [[ "$(docker logs --since "${CAPABILITY_MAX_AGE_MINUTES}m" "$UPDATER_CONTAINER" 2>&1 || true)" == *"$CAPABILITY_ID"* ]]; then
    pass "capability" "log line of $UPDATER_CONTAINER names $CAPABILITY_ID (settings row missing, e.g. after the nightly reset)"
  else
    fail "capability" "no capability row and no log line for $CAPABILITY_ID in the last $CAPABILITY_MAX_AGE_MINUTES min"
  fi
}

check_release_dependent() {
  if [ "$pre_release" -eq 1 ]; then
    skip "manifest, updater tag, capability" "--pre-release"
    return 0
  fi
  check_manifest
  check_updater_and_capability
}

run_preflight() {
  check_window "$UPGRADE_RESERVE_MINUTES"
  if crontab -l 2> /dev/null | grep -q "^${CRON_MARKER}"; then
    warn "reset cron" "crontab still has lines marked '${CRON_MARKER}' from an interrupted run: the nightly reset is suspended"
  fi
  check_disk
  check_database_state
  check_signing_key
  check_compose
  check_release_dependent
  if [ "${#FAILS[@]}" -eq 0 ]; then
    take_snapshots
    take_backups
  else
    skip "snapshot and backup" "an earlier check failed"
  fi
  if [ "${#WARNS[@]}" -gt 0 ]; then log "Warnings: ${WARNS[*]}"; fi
  if [ "${#FAILS[@]}" -gt 0 ]; then
    log "PREFLIGHT FAILED: ${FAILS[*]}"
    return 1
  fi
  log "PREFLIGHT OK"
}

acquire_deploy_lock() {
  exec 201> "$DEPLOY_LOCK"
  flock -n 201 || die "a deploy holds $DEPLOY_LOCK, try again when it is done"
}

require_confirm() {
  [ "$confirm" = "$1" ] || die "this mode needs --confirm $1"
}

suspend_reset_cron() {
  local current
  current="$(crontab -l 2> /dev/null || true)"
  printf '%s\n' "$current" > "$BACKUP_DIR/crontab.before-$timestamp"
  if ! printf '%s\n' "$current" | grep -q "$CRON_RESET_PATTERN"; then
    warn "reset cron" "no crontab line with $CRON_RESET_PATTERN found, nothing to suspend"
    return 0
  fi
  printf '%s\n' "$current" | sed "/^[[:space:]]*#/! s|^\(.*${CRON_RESET_PATTERN}.*\)\$|${CRON_MARKER}\1|" | crontab -
  cron_suspended=1
  log "Reset cron suspended (lines marked with '${CRON_MARKER}'); restored on exit."
}

operation_status() { # id
  psql_db "$APP_DB" "SELECT status FROM update_history WHERE id = '$1'" 2> /dev/null | tr -d '[:space:]'
}

api_healthy_within() { wait_until "$1" api_healthy; }

verify_upgraded() {
  local major db before_dbs after_dbs count invalid code version
  major="$(pg_major || true)"
  if [ "$major" = "$to_major" ]; then pass "version" "server major $major"; else fail "version" "server major '${major:-unknown}', expected $to_major"; fi

  before_dbs="$(LC_ALL=C sort "$snapshot_prefix.databases" | paste -sd, -)"
  after_dbs="$(list_databases | LC_ALL=C sort | paste -sd, -)"
  if [ "$before_dbs" = "$after_dbs" ]; then pass "databases" "$after_dbs"; else fail "databases" "after '$after_dbs', before '$before_dbs'"; fi

  while IFS= read -r db; do
    [ -n "$db" ] || continue
    if ! snapshot_counts "$db" "$work/after.$db.tsv"; then
      fail "row counts $db" "cannot read"
      continue
    fi
    if compare_counts "$snapshot_prefix.$db.tsv" "$work/after.$db.tsv" > "$work/diff.$db"; then
      pass "row counts $db" "no table lost rows (writes between snapshot and switch only add)"
    else
      fail "row counts $db" "$(paste -sd';' - < "$work/diff.$db")"
    fi
  done < "$snapshot_prefix.databases"

  count="$(psql_q "$APP_DB" "SELECT count(*) FROM pg_extension WHERE extname IN ('vector', 'pg_trgm')" | tr -d '[:space:]')"
  if [ "$count" = "2" ]; then pass "extensions" "vector and pg_trgm present"; else fail "extensions" "'${count:-unreadable}' of 2 present"; fi
  invalid="$(psql_q "$APP_DB" "SELECT count(*) FROM pg_index WHERE NOT indisvalid" | tr -d '[:space:]')"
  if [ "$invalid" = "0" ]; then pass "indexes" "no invalid index"; else fail "indexes" "'${invalid:-unreadable}' invalid index(es)"; fi

  if api_healthy_within "$HEALTH_WAIT_SECONDS"; then
    pass "api health" "healthy"
    version="$(api_exec_curl /api/Version 2> /dev/null | python3 -c 'import json,sys; print(json.load(sys.stdin).get("VersionString", "?"))' 2> /dev/null || echo unreadable)"
    log "API version: $version"
    code="$(api_exec_curl /api/backend/Update/Status -o /dev/null -w '%{http_code}' 2> /dev/null || true)"
    if [ "$code" = "$EXPECTED_AUTH_CODE" ]; then pass "auth smoke" "a protected endpoint answers $EXPECTED_AUTH_CODE without a token"; else fail "auth smoke" "a protected endpoint answered '$code', expected $EXPECTED_AUTH_CODE"; fi
  else
    fail "api health" "not healthy within ${HEALTH_WAIT_SECONDS}s"
  fi
  [ "${#FAILS[@]}" -eq 0 ]
}

verify_old_engine_back() {
  local major
  if ! wait_until "$PG_READY_WAIT_SECONDS" pg_ready; then
    fail "old engine" "postgres not ready"
    return 1
  fi
  major="$(pg_major || true)"
  if [ "$major" = "$from_major" ]; then
    pass "old engine" "server major $major is running"
  else
    fail "old engine" "server major '${major:-unknown}', expected $from_major"
    return 1
  fi
  if api_healthy_within "$HEALTH_WAIT_SECONDS"; then
    pass "api health" "healthy"
  else
    fail "api health" "not healthy within ${HEALTH_WAIT_SECONDS}s"
    return 1
  fi
}

old_cluster_version() {
  local version
  if container_running "$PG_CONTAINER"; then
    version="$(docker exec "$PG_CONTAINER" cat "$PGDATA_ROOT/pgdata/PG_VERSION" 2> /dev/null || true)"
  else
    version="$(docker run --rm --network none --entrypoint cat -v "${PROJECT}_postgres_data:/data:ro" "$OLD_IMAGE" /data/pgdata/PG_VERSION 2> /dev/null || true)"
  fi
  printf '%s' "$version" | tr -d '[:space:]'
}

restore_pg_env() {
  local key value tmp
  for key in "${PG_ENV_KEYS[@]}"; do
    value="$(grep -m1 -E "^[[:space:]]*${key}=" "$APPS_DIR/.env.pre-pg19" || true)"
    tmp="$(mktemp "$ENV_FILE.XXXXXX")"
    grep -v -E "^[[:space:]]*${key}=" "$ENV_FILE" > "$tmp" || true
    if [ -s "$tmp" ] && [ -n "$(tail -c1 "$tmp")" ]; then echo >> "$tmp"; fi
    if [ -n "$value" ]; then printf '%s\n' "$value" >> "$tmp"; fi
    cat "$tmp" > "$ENV_FILE"
    rm -f "$tmp"
    if [ -n "$value" ]; then log "$key restored from .env.pre-pg19"; else log "$key removed (it was not set before the switch)"; fi
  done
}

compose_cmd() {
  docker compose --project-name "$PROJECT" --project-directory "$APPS_DIR" --env-file "$ENV_FILE" -f "$APPS_DIR/docker-compose.yml" "$@"
}

do_rollback() {
  [ ! -e "$BACKUP_DIR/cleanup-done" ] || die "cleanup was already done, the old cluster is gone: rollback is impossible (last resort: $RESET_SCRIPT restores the golden dump)"
  [ -f "$APPS_DIR/.env.pre-pg19" ] || die "$APPS_DIR/.env.pre-pg19 not found, nothing to restore"
  local old_version
  old_version="$(old_cluster_version)"
  [ "$old_version" = "$from_major" ] || die "the old cluster $PGDATA_ROOT/pgdata holds PG_VERSION '${old_version:-none}', not $from_major: rollback would start an empty cluster, nothing is changed"
  log "WARNING: everything written since the switch is lost; the old cluster is returned exactly as it was before."
  compose_cmd stop "$UPDATER_CONTAINER" "$API_CONTAINER" || log "stop of updater and api returned non-zero, continuing"
  cp -p "$ENV_FILE" "$BACKUP_DIR/env-before-rollback-$timestamp"
  chmod 600 "$BACKUP_DIR/env-before-rollback-$timestamp"
  restore_pg_env
  compose_cmd up -d postgres
  wait_until "$PG_READY_WAIT_SECONDS" pg_ready || die "old postgres did not become ready; last resort: $RESET_SCRIPT (restores the golden dump)"
  psql_db "$APP_DB" "UPDATE update_history SET status = $STATUS_ROLLED_BACK, completed_at = now(), message = 'Rolled back manually by hetzner-engine-upgrade rollback' WHERE operation_type = $OPERATION_TYPE_POSTGRES_UPGRADE AND $STATUS_ACTIVE_SQL" || log "could not close the engine operation row"
  compose_cmd up -d "$API_CONTAINER" "$UPDATER_CONTAINER"
  verify_old_engine_back || die "rollback verification failed: ${FAILS[*]}"
  log "ROLLBACK OK: PostgreSQL $from_major is running again."
}

wait_for_operation() { # id -> prints the terminal status, or the last seen status after the timeout
  local waited=0 status="" outage=0 seen=""
  while [ "$waited" -lt "$UPGRADE_WAIT_SECONDS" ]; do
    sleep "$POLL_SECONDS"
    waited=$((waited + POLL_SECONDS))
    if status="$(operation_status "$1")" && [[ "$status" =~ ^[0-9]+$ ]]; then
      outage=0
      seen="$status"
      case "$status" in
        "$STATUS_SUCCEEDED" | "$STATUS_FAILED" | "$STATUS_ROLLED_BACK" | "$STATUS_CANCELLED" | "$STATUS_ROLLBACK_FAILED")
          echo "$status"
          return 0
          ;;
      esac
    else
      outage=$((outage + 1))
      log "Database not answering (poll $outage; the switch stops it briefly)." >&2
    fi
  done
  echo "timeout:${seen:-unknown}"
}

mode_upgrade() {
  local image sha sig op_id result
  require_confirm "$CONFIRM_UPGRADE"
  [ "$pre_release" -eq 0 ] || die "--pre-release is for preflight only"
  if [ -z "$enqueue_script" ] || [ ! -f "$enqueue_script" ]; then die "--enqueue helper (deploy/postgres/enqueue-engine-upgrade.sh) not found"; fi
  acquire_deploy_lock
  mkdir -p "$BACKUP_DIR"
  chmod 700 "$BACKUP_DIR"
  run_preflight || die "preflight failed, nothing was changed"

  { read -r image; read -r sha; read -r sig; } < "$work/engine.values"
  install -m 600 "$ENV_FILE" "$APPS_DIR/.env.pre-pg19"
  suspend_reset_cron

  log "Enqueueing the engine upgrade $from_major -> $to_major ($image)."
  op_id="$(bash "$enqueue_script" --psql "docker exec -i $PG_CONTAINER psql -U $PG_USER -d $APP_DB -X" --from "$from_major" --to "$to_major" \
    --ref "$image" --sha "$sha" --sig "$sig" --requested-by "hetzner-engine-upgrade:$run_id" | tail -n1 | tr -d '[:space:]')" \
    || die "enqueue helper failed (an operation may already be active)"
  [[ "$op_id" =~ $UUID_PATTERN ]] || die "enqueue helper did not print an operation id (got '$op_id')"
  log "Operation $op_id enqueued, waiting up to $((UPGRADE_WAIT_SECONDS / 60)) min."

  result="$(wait_for_operation "$op_id")"
  case "$result" in
    "$STATUS_SUCCEEDED")
      log "Operation $op_id Succeeded, verifying."
      verify_upgraded || die "the upgrade reported Succeeded but verification failed: ${FAILS[*]}. Decide on 'rollback' (data written since the switch would be lost)."
      date +%s > "$BACKUP_DIR/$UPGRADE_MARKER_NAME"
      log "UPGRADE OK: PostgreSQL $to_major is live. The old cluster stays for rollback until 'cleanup'."
      ;;
    "$STATUS_FAILED" | "$STATUS_ROLLED_BACK" | "$STATUS_CANCELLED")
      log "Operation $op_id ended with status $result; the updater kept or restored the old engine."
      verify_old_engine_back || true
      die "the engine upgrade did not complete (status $result); old engine check: ${FAILS[*]:-ok}"
      ;;
    *)
      log "Operation $op_id ended as '$result' (RollbackFailed or no terminal status within $((UPGRADE_WAIT_SECONDS / 60)) min): running the manual rollback."
      do_rollback || true
      die "the engine upgrade failed or timed out; the manual rollback was attempted (see above)"
      ;;
  esac
}

mode_rollback() {
  require_confirm "$CONFIRM_ROLLBACK"
  acquire_deploy_lock
  mkdir -p "$BACKUP_DIR"
  do_rollback
}

mode_regenerate_golden() {
  local major golden="$GOLDEN_DIR/golden.dump" fresh="$GOLDEN_DIR/golden.dump.new-$timestamp" previous probe
  require_confirm "$CONFIRM_GOLDEN"
  acquire_deploy_lock
  major="$(pg_major || true)"
  [ "$major" = "$to_major" ] || die "server major is '${major:-unknown}', the golden dump is regenerated from PostgreSQL $to_major only"
  if window_blocked "$UPGRADE_RESERVE_MINUTES"; then die "inside the blocked window 01:15-04:00 UTC or closer than $UPGRADE_RESERVE_MINUTES min to its start"; fi
  [ -f "$RESET_SCRIPT" ] || die "$RESET_SCRIPT not found"
  [ -f "$golden" ] || die "$golden not found"
  previous="$golden.pre-pg19-$timestamp"
  [ ! -e "$previous" ] || die "$previous already exists"
  probe="${APP_DB}_goldentest"

  log "Running the reset (restores the old golden dump into PostgreSQL $to_major, restarts the API)."
  bash "$RESET_SCRIPT" || die "the reset script failed, the old golden dump is kept"
  api_healthy_within "$GOLDEN_HEALTH_WAIT_SECONDS" || die "the API is not healthy within $((GOLDEN_HEALTH_WAIT_SECONDS / 60)) min after the reset, the old golden dump is kept"

  (
    umask 077
    docker exec "$PG_CONTAINER" pg_dump -U "$PG_USER" -Fc -d "$APP_DB" > "$fresh"
  ) || { rm -f "$fresh"; die "pg_dump failed, the old golden dump is kept"; }
  [ "$(stat -c %s "$fresh")" -ge "$MIN_DUMP_BYTES" ] || { rm -f "$fresh"; die "the new dump is implausibly small, the old golden dump is kept"; }
  docker exec -i "$PG_CONTAINER" pg_restore -l < "$fresh" > "$work/golden.toc" || { rm -f "$fresh"; die "pg_restore -l rejects the new dump, the old golden dump is kept"; }
  grep -q 'TABLE DATA' "$work/golden.toc" || { rm -f "$fresh"; die "the new dump has no table data, the old golden dump is kept"; }

  docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d postgres -X -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS \"$probe\" WITH (FORCE)" -c "CREATE DATABASE \"$probe\""
  if ! docker exec -i "$PG_CONTAINER" pg_restore -U "$PG_USER" -d "$probe" --no-owner --exit-on-error < "$fresh"; then
    docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d postgres -X -c "DROP DATABASE IF EXISTS \"$probe\" WITH (FORCE)" || true
    rm -f "$fresh"
    die "the probe restore of the new dump failed, the old golden dump is kept"
  fi
  docker exec "$PG_CONTAINER" psql -U "$PG_USER" -d postgres -X -v ON_ERROR_STOP=1 -c "DROP DATABASE \"$probe\" WITH (FORCE)"

  cp -p "$golden" "$previous"
  mv "$fresh" "$golden"
  log "golden.dump regenerated from PostgreSQL $to_major ($(stat -c %s "$golden") bytes); the previous one is $previous."
  printf '%s regenerated from PostgreSQL %s by run %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$to_major" "$run_id" >> "$GOLDEN_DIR/golden-notes.log"
}

mode_cleanup() {
  local major subdir age succeeded_at old_version
  require_confirm "$CONFIRM_CLEANUP"
  acquire_deploy_lock
  if window_blocked 0; then die "inside the blocked window 01:15-04:00 UTC"; fi
  major="$(pg_major || true)"
  [ "$major" = "$to_major" ] || die "server major is '${major:-unknown}', cleanup needs PostgreSQL $to_major running"
  subdir="$(env_value KLACKS_POSTGRES_DATA_SUBDIR)"
  [ "$subdir" = "$new_subdir" ] || die "KLACKS_POSTGRES_DATA_SUBDIR is '${subdir:-unset}', expected $new_subdir: the new cluster is not the active one"
  docker exec "$PG_CONTAINER" printenv PGDATA | grep -qx "$PGDATA_ROOT/$new_subdir" || die "the running container does not use $PGDATA_ROOT/$new_subdir"
  succeeded_at="$(cat "$BACKUP_DIR/$UPGRADE_MARKER_NAME" 2> /dev/null || true)"
  [[ "$succeeded_at" =~ ^[0-9]+$ ]] || die "$BACKUP_DIR/$UPGRADE_MARKER_NAME not found: the upgrade mode writes it when the upgrade succeeded"
  age=$(($(date +%s) - succeeded_at))
  age=$((age / SECONDS_PER_DAY))
  [ "$age" -ge "$CLEANUP_MIN_AGE_DAYS" ] || die "the upgrade is $age day(s) old, cleanup waits for $CLEANUP_MIN_AGE_DAYS"
  api_healthy || die "the API is not healthy, no cleanup now"

  old_version="$(old_cluster_version)"
  [ "$old_version" = "$from_major" ] || die "$PGDATA_ROOT/pgdata holds PG_VERSION '${old_version:-none}', not the old $from_major cluster: nothing is deleted"

  log "Deleting the old cluster $PGDATA_ROOT/pgdata (PostgreSQL $old_version), the exchange directory and the old image."
  mkdir -p "$BACKUP_DIR"
  printf '%s run %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$run_id" > "$BACKUP_DIR/cleanup-done"
  docker exec "$PG_CONTAINER" rm -rf "$PGDATA_ROOT/pgdata" "$PGDATA_ROOT/engine-upgrade"
  docker image rm "$OLD_IMAGE" > /dev/null 2>&1 || log "old image $OLD_IMAGE not removed (still referenced or already gone)"
  log "CLEANUP OK. Rollback is no longer possible."
}

case "$mode" in
  preflight) run_preflight ;;
  upgrade) mode_upgrade ;;
  rollback) mode_rollback ;;
  regenerate-golden) mode_regenerate_golden ;;
  cleanup) mode_cleanup ;;
  *) die "--mode must be preflight, upgrade, rollback, regenerate-golden or cleanup" ;;
esac
