#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Engine upgrade gate, Docker on-prem driver (Linux runner). Called step by step by
# .github/workflows/engine-upgrade-gate.yml; every subcommand is one step of a scenario:
#   install-baseline      released on-prem bundle (GATE_BASELINE_VERSION, PostgreSQL 17), first start, own admin
#   apply-candidate       candidate compose + install.sh from this repository over it (postgres container must stay),
#                         candidate updater image sha-<GATE_UPDATER_SHA> with the CI public key (compose override)
#   seed                  extra databases, gate vectors (HNSW), filler rows
#   calibrate             restart the API once and record which Klacks tables change on their own (volatile)
#   snapshot <label>      row counts per table, extensions, invalid indexes, exact KNN lists
#   compare <a> <b>       compares two snapshots (all tables except volatile Klacks tables; KNN lists identical)
#   enqueue               queues the signed type-4 row through deploy/postgres/enqueue-engine-upgrade.sh
#   kill-during-restore   kills klacks-updater while the new engine is active and before the commit, starts it again
#   wait-terminal <s,..>  waits for the row to finish and checks the status (2 Succeeded, 3 Failed, 4 RolledBack)
#   assert-success        PostgreSQL GATE_TO_MAJOR, extensions, indexes, HNSW, old cluster kept, .env + signed digest
#   assert-rollback       PostgreSQL GATE_FROM_MAJOR again, .env pin restored, failed target folder removed
#   restart-stack         docker compose down + up -d, healthy again on the same engine
#   installer-rerun       candidate install.sh once more: postgres container and pin unchanged
#   fresh-install         candidate bundle on an empty machine: PostgreSQL GATE_TO_MAJOR from the start
#   diagnostics <dir>     logs, journal, update rows (never .env)
#
# Environment: GATE_REPO_DIR (Klacks.Api checkout), GATE_STATE_DIR, GATE_INSTALL_DIR, GATE_BASELINE_VERSION,
# GATE_UPDATER_SHA, GATE_PUBLIC_KEY_FILE (CI public key), GATE_FAULT_PHASE (optional), GATE_ADMIN_EMAIL,
# GATE_ADMIN_PASSWORD, GATE_FROM_MAJOR, GATE_TO_MAJOR, GATE_EXPECTED_VECTOR, GATE_ENGINE_REF, GATE_ENGINE_SHA,
# GATE_SIGNATURE_FILE, GATE_FILLER_ROWS.
set -euo pipefail

readonly POSTGRES_CONTAINER='klacks-postgres'
readonly UPDATER_CONTAINER='klacks-updater'
readonly API_CONTAINER='klacks-api'
readonly HEALTHCHECKED_CONTAINERS='klacks-postgres klacks-api klacks-ui klacks-proxy'
readonly APP_URL='https://localhost'
readonly POSTGRES_USER='admin'
readonly KLACKS_DATABASE='Klacks'
readonly SIDE_DATABASES='klacks_diag klacks_telemetry'
readonly FILLER_DATABASE='klacks_diag'
readonly VOLUME_ROOT='/var/lib/postgresql/data'
readonly LEGACY_SUBDIR='pgdata'
readonly LEGACY_IMAGE='pgvector/pgvector:pg17'
readonly IMAGE_ENV='KLACKS_POSTGRES_IMAGE'
readonly SUBDIR_ENV='KLACKS_POSTGRES_DATA_SUBDIR'
readonly UPDATER_TAG_ENV='KLACKS_UPDATER_TAG'
readonly OVERRIDE_FILE='docker-compose.override.yml'
readonly CONTAINER_GATE_DIR='/tmp/klacks-engine-gate'
readonly JOURNAL_DIR='/backups/engine-upgrade'
readonly CAPABILITY='postgres-engine-upgrade/1'
readonly FIXED_VOLATILE_TABLES='public.update_history'
readonly GATE_TABLE_PREFIX='public.engine_gate_'
readonly RELEASE_DOWNLOAD_BASE='https://github.com/HeribertG/Klacks.Api/releases/download'
readonly VERSION_PATH='/api/Version'
readonly FIRST_START_STATUS_PATH='/api/backend/Setup/FirstStartStatus'
readonly COMPLETE_FIRST_START_PATH='/api/backend/Setup/CompleteFirstStart'
readonly LOGIN_PATH='/api/backend/Accounts/LoginUser'
readonly SETUP_CODE_LINE_MARKER='Your setup code:'
readonly HEALTH_TIMEOUT_SECONDS=1800
readonly HEALTH_POLL_SECONDS=10
readonly CAPABILITY_TIMEOUT_SECONDS=300
readonly UPGRADE_TIMEOUT_SECONDS=2700
readonly UPGRADE_POLL_SECONDS=10
readonly KILL_POLL_SECONDS=0.2
readonly CALIBRATION_IDLE_SECONDS=60
readonly CURL_OPTIONS=(--connect-timeout 15 --max-time 120)
readonly STATUS_SUCCEEDED=2
readonly STATUS_FAILED=3
readonly STATUS_ROLLED_BACK=4
readonly STATUS_ROLLBACK_FAILED=6
readonly KNN_LIST_COUNT=20
readonly EXIT_OPERATION_ACTIVE=3

GATES_DIR="$(cd "$(dirname "$0")" && pwd)"
readonly GATES_DIR

fail() { printf '::error::%s\n' "$1" >&2; exit 1; }
note() { printf '==> %s\n' "$1"; }
require() { local name; for name in "$@"; do [ -n "${!name:-}" ] || fail "$name is not set."; done; }

compose() { (cd "$GATE_INSTALL_DIR" && sudo docker compose "$@"); }

env_get() {
  sudo sh -c "grep -m1 '^$1=' '$GATE_INSTALL_DIR/.env' | cut -d= -f2-" || true
}

env_set() {
  local key="$1" value="$2" file="$GATE_INSTALL_DIR/.env"
  if sudo grep -q "^$key=" "$file"; then
    sudo sed -i "s|^$key=.*|$key=$value|" "$file"
  else
    printf '%s=%s\n' "$key" "$value" | sudo tee -a "$file" >/dev/null
  fi
}

container_id() { sudo docker inspect --format '{{.Id}}' "$1" 2>/dev/null || echo missing; }

# Copies the gate SQL into the (possibly new) postgres container, so \ir includes resolve there.
sync_sql() {
  sudo docker exec "$POSTGRES_CONTAINER" mkdir -p "$CONTAINER_GATE_DIR"
  sudo docker cp "$GATES_DIR/." "$POSTGRES_CONTAINER:$CONTAINER_GATE_DIR/" >/dev/null
}

# psql_file <database> <file in gates dir> [psql options...]
psql_file() {
  local database="$1" file="$2"
  shift 2
  sudo docker exec "$POSTGRES_CONTAINER" psql -X -q -v ON_ERROR_STOP=1 -At -U "$POSTGRES_USER" -d "$database" "$@" \
    -f "$CONTAINER_GATE_DIR/$file"
}

psql_query() {
  local database="$1" sql="$2"
  printf '%s\n' "$sql" | sudo docker exec -i "$POSTGRES_CONTAINER" psql -X -q -v ON_ERROR_STOP=1 -At -U "$POSTGRES_USER" -d "$database"
}

wait_healthy() {
  local deadline=$(( $(date +%s) + HEALTH_TIMEOUT_SECONDS )) pending container state
  while :; do
    pending=''
    for container in $HEALTHCHECKED_CONTAINERS; do
      state="$(sudo docker inspect --format '{{.State.Status}}/{{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$container" 2>/dev/null || echo missing/missing)"
      case "$state" in
        running/healthy) ;;
        exited/*|dead/*) fail "$container is $state." ;;
        *) pending="$pending $container=$state" ;;
      esac
    done
    [ -z "$pending" ] && break
    [ "$(date +%s)" -lt "$deadline" ] || fail "Not healthy after ${HEALTH_TIMEOUT_SECONDS}s:$pending"
    echo "waiting:$pending"
    sleep "$HEALTH_POLL_SECONDS"
  done
  curl -k -fsS "${CURL_OPTIONS[@]}" "$APP_URL$VERSION_PATH" >/dev/null || fail "$APP_URL$VERSION_PATH does not answer."
  note 'All containers healthy, the API answers.'
}

api_login() {
  local body status
  body="$(jq -n --arg e "$GATE_ADMIN_EMAIL" --arg p "$GATE_ADMIN_PASSWORD" '{email:$e,password:$p}')"
  status="$(curl -k -sS "${CURL_OPTIONS[@]}" -o "$GATE_STATE_DIR/login.json" -w '%{http_code}' -H 'Content-Type: application/json' \
    --data "$body" "$APP_URL$LOGIN_PATH")"
  [ "$status" = 200 ] || fail "Sign-in of the gate admin failed (HTTP $status)."
  [ "$(jq -r '.token // empty | length > 0' "$GATE_STATE_DIR/login.json")" = true ] || fail 'Sign-in returned no token.'
  rm -f "$GATE_STATE_DIR/login.json"
  note 'Gate admin signed in.'
}

# First start of a fresh installation with the setup code from .env; creates the gate admin.
first_start() {
  local setup_code status_body registration status
  setup_code="$(env_get KLACKS_SETUP_CODE)"
  [ -n "$setup_code" ] || fail 'install.sh wrote no KLACKS_SETUP_CODE.'
  echo "::add-mask::$setup_code"
  status_body="$(curl -k -fsS "${CURL_OPTIONS[@]}" "$APP_URL$FIRST_START_STATUS_PATH")"
  [ "$(jq -r '.requiresOwnAdmin' <<<"$status_body")" = true ] || fail 'A fresh installation does not ask for the own admin.'
  registration="$(jq -n --arg e "$GATE_ADMIN_EMAIL" --arg p "$GATE_ADMIN_PASSWORD" --arg c "$setup_code" \
    '{email:$e,firstName:"Engine",lastName:"Gate",password:$p,setupCode:$c}')"
  status="$(curl -k -sS "${CURL_OPTIONS[@]}" -o /dev/null -w '%{http_code}' -H 'Content-Type: application/json' \
    --data "$registration" "$APP_URL$COMPLETE_FIRST_START_PATH")"
  [ "$status" = 200 ] || fail "First start with the setup code failed (HTTP $status)."
  api_login
}

run_installer() {
  local log="$1"
  (cd "$GATE_INSTALL_DIR" && sudo bash ./install.sh </dev/null 2>&1) | sed -E "s/($SETUP_CODE_LINE_MARKER +)[A-Z0-9-]+/\1***/" | tee "$log"
}

disable_auto_update() {
  psql_query "$KLACKS_DATABASE" "UPDATE settings SET value = 'False' WHERE type = 'UPDATE_AUTO_ENABLED';" >/dev/null
  note 'Automatic app updates switched off (the gate must be the only one queueing operations).'
}

write_override() {
  local key_lines fault_line=''
  key_lines="$(sed 's/^/        /' "$GATE_PUBLIC_KEY_FILE")"
  if [ -n "${GATE_FAULT_PHASE:-}" ]; then
    [[ "$GATE_FAULT_PHASE" =~ ^[A-Za-z]+$ ]] || fail "GATE_FAULT_PHASE '$GATE_FAULT_PHASE' is not a phase name."
    fault_line="      Updater__EngineUpgradeFaultPhase: $GATE_FAULT_PHASE"
  fi
  sudo tee "$GATE_INSTALL_DIR/$OVERRIDE_FILE" >/dev/null <<EOF
# Engine upgrade gate only: the candidate updater trusts the throw-away CI key (never the production key).
services:
  klacks-updater:
    environment:
      Updater__SignaturePublicKey: |
$key_lines
$fault_line
EOF
}

wait_capability() {
  local deadline=$(( $(date +%s) + CAPABILITY_TIMEOUT_SECONDS )) value
  while :; do
    value="$(psql_query "$KLACKS_DATABASE" "SELECT value FROM settings WHERE type = 'UPDATER_CAPABILITIES' LIMIT 1;" 2>/dev/null || true)"
    if [[ "$value" == *"$CAPABILITY"* ]] && [[ "$value" == *"$1"* ]]; then
      note "Updater capability: $value"
      return 0
    fi
    [ "$(date +%s)" -lt "$deadline" ] || fail "No UPDATER_CAPABILITIES row with $CAPABILITY and mode $1 after ${CAPABILITY_TIMEOUT_SECONDS}s (candidate updater not running?)."
    sleep "$HEALTH_POLL_SECONDS"
  done
}

start_candidate_updater() {
  env_set "$UPDATER_TAG_ENV" "sha-$GATE_UPDATER_SHA"
  write_override
  compose pull "$UPDATER_CONTAINER"
  compose up -d --no-deps "$UPDATER_CONTAINER"
  local image
  image="$(sudo docker inspect --format '{{.Config.Image}}' "$UPDATER_CONTAINER")"
  [ "$image" = "ghcr.io/heribertg/klacks-updater:sha-$GATE_UPDATER_SHA" ] || fail "klacks-updater runs $image."
}

copy_candidate_bundle() {
  local target="$1"
  [ -f "$GATE_REPO_DIR/deploy/onprem/docker-compose.yml" ] || fail 'deploy/onprem/docker-compose.yml missing in the checkout.'
  grep -q "$IMAGE_ENV" "$GATE_REPO_DIR/deploy/onprem/docker-compose.yml" \
    || fail "The candidate compose does not use $IMAGE_ENV (engine parametrisation not merged)."
  sudo mkdir -p "$target"
  sudo cp -R "$GATE_REPO_DIR/deploy/onprem/." "$target/"
}

cmd_install_baseline() {
  require GATE_INSTALL_DIR GATE_BASELINE_VERSION GATE_STATE_DIR GATE_ADMIN_EMAIL GATE_ADMIN_PASSWORD
  local parent zip
  parent="$(dirname "$GATE_INSTALL_DIR")"
  mkdir -p "$parent" "$GATE_STATE_DIR"
  zip="$GATE_STATE_DIR/klacks-onprem-$GATE_BASELINE_VERSION.zip"
  curl -fsSL --retry 3 -o "$zip" "$RELEASE_DOWNLOAD_BASE/v$GATE_BASELINE_VERSION/klacks-onprem.zip"
  unzip -q -o "$zip" -d "$parent"
  [ -f "$GATE_INSTALL_DIR/install.sh" ] || fail "The baseline bundle has no install.sh in $GATE_INSTALL_DIR."
  grep -q "image: $LEGACY_IMAGE" "$GATE_INSTALL_DIR/docker-compose.yml" || fail "The baseline compose does not run $LEGACY_IMAGE."
  # Pinned before the first run: install.sh keeps existing values, so the baseline never follows stable.json and the
  # API never queues an app update during the gate.
  cat > "$GATE_INSTALL_DIR/.env" <<EOF
KLACKS_API_TAG=$GATE_BASELINE_VERSION
KLACKS_UI_TAG=$GATE_BASELINE_VERSION
$UPDATER_TAG_ENV=$GATE_BASELINE_VERSION
UPDATE_AUTO_ENABLED=false
UPDATE_NOTIFY_ONLY=true
EOF
  run_installer "$GATE_STATE_DIR/install-baseline.log"
  wait_healthy
  first_start
  disable_auto_update
  container_id "$POSTGRES_CONTAINER" > "$GATE_STATE_DIR/postgres-container-id"
  docker inspect --format '{{.Image}}' "$POSTGRES_CONTAINER" > "$GATE_STATE_DIR/postgres-image-id"
  docker image inspect --format '{{join .RepoDigests ","}}' "$LEGACY_IMAGE" > "$GATE_STATE_DIR/postgres-repo-digests"
  psql_query "$KLACKS_DATABASE" 'SHOW server_version;'
}

cmd_apply_candidate() {
  require GATE_INSTALL_DIR GATE_REPO_DIR GATE_STATE_DIR GATE_UPDATER_SHA GATE_PUBLIC_KEY_FILE
  local before after remote local_digests
  before="$(cat "$GATE_STATE_DIR/postgres-container-id")"
  local_digests="$(cat "$GATE_STATE_DIR/postgres-repo-digests")"
  remote="$(docker buildx imagetools inspect "$LEGACY_IMAGE" --format '{{json .Manifest}}' | jq -r '.digest')"
  note "$LEGACY_IMAGE: local $local_digests, registry $remote"
  if [[ "$local_digests" != *"$remote"* ]]; then
    echo "::warning::$LEGACY_IMAGE moved in the registry since the baseline install; install.sh pulls it and compose recreates postgres."
  fi
  copy_candidate_bundle "$GATE_INSTALL_DIR"
  run_installer "$GATE_STATE_DIR/install-candidate.log"
  wait_healthy
  after="$(container_id "$POSTGRES_CONTAINER")"
  if [ "$after" != "$before" ]; then
    note "image before: $(cat "$GATE_STATE_DIR/postgres-image-id"), now: $(sudo docker inspect --format '{{.Image}}' "$POSTGRES_CONTAINER")"
    fail 'The candidate install.sh recreated the postgres container (compose config hash or image changed).'
  fi
  [ "$(env_get "$IMAGE_ENV")" = "$LEGACY_IMAGE" ] || fail ".env pins $IMAGE_ENV='$(env_get "$IMAGE_ENV")', expected $LEGACY_IMAGE."
  [ "$(env_get "$SUBDIR_ENV")" = "$LEGACY_SUBDIR" ] || fail ".env pins $SUBDIR_ENV='$(env_get "$SUBDIR_ENV")', expected $LEGACY_SUBDIR."
  note "Candidate installer kept postgres ($after) and pinned $LEGACY_IMAGE / $LEGACY_SUBDIR."
  start_candidate_updater
  [ "$(container_id "$POSTGRES_CONTAINER")" = "$before" ] || fail 'Starting the candidate updater recreated postgres.'
  wait_capability Docker
}

cmd_seed() {
  require GATE_FILLER_ROWS
  sync_sql
  psql_file "$KLACKS_DATABASE" seed-databases.sql
  psql_file "$KLACKS_DATABASE" seed-gate-vectors.sql
  local database rows
  for database in $SIDE_DATABASES; do
    rows=0
    [ "$database" = "$FILLER_DATABASE" ] && rows="$GATE_FILLER_ROWS"
    psql_file "$database" seed-side-database.sql -v "filler_rows=$rows"
  done
  disable_auto_update
}

snapshot_rows() {
  local database
  sync_sql
  for database in $KLACKS_DATABASE $SIDE_DATABASES; do
    psql_file "$database" snapshot-database.sql | tr -d '\r' | sed "s/^/$database|/"
  done
}

cmd_calibrate() {
  require GATE_STATE_DIR
  snapshot_rows > "$GATE_STATE_DIR/calibration-0.txt"
  compose restart "$API_CONTAINER"
  wait_healthy
  api_login
  sleep "$CALIBRATION_IDLE_SECONDS"
  snapshot_rows > "$GATE_STATE_DIR/calibration-1.txt"
  {
    for table in $FIXED_VOLATILE_TABLES; do echo "$table"; done
    # diff exits 1 when tables changed, which is exactly the case collected here.
    { diff <(grep "^$KLACKS_DATABASE|rows|" "$GATE_STATE_DIR/calibration-0.txt") \
           <(grep "^$KLACKS_DATABASE|rows|" "$GATE_STATE_DIR/calibration-1.txt") || true; } \
      | sed -n 's/^[<>] [^|]*|rows|\([^|]*\)|.*/\1/p'
  } | grep -v "^$GATE_TABLE_PREFIX" | sort -u > "$GATE_STATE_DIR/volatile-tables.txt"
  note "Klacks tables that change without an upgrade (excluded from the row-count comparison):"
  cat "$GATE_STATE_DIR/volatile-tables.txt"
}

cmd_snapshot() {
  local label="${1:?snapshot label}"
  require GATE_STATE_DIR
  snapshot_rows > "$GATE_STATE_DIR/$label-rows.txt"
  psql_file "$KLACKS_DATABASE" snapshot-knn.sql | tr -d '\r' > "$GATE_STATE_DIR/$label-knn.txt"
  [ "$(grep -c '^knn|' "$GATE_STATE_DIR/$label-knn.txt")" = "$KNN_LIST_COUNT" ] || fail "Snapshot $label has no $KNN_LIST_COUNT KNN lists."
  note "Snapshot $label: $(grep -c '|rows|' "$GATE_STATE_DIR/$label-rows.txt") tables."
}

comparable_rows() {
  local file="$1" volatile="$GATE_STATE_DIR/volatile-tables.txt"
  grep '|rows|' "$file" | while IFS='|' read -r database _ table count; do
    if [ "$database" = "$KLACKS_DATABASE" ] && grep -qxF "$table" "$volatile"; then continue; fi
    printf '%s|%s|%s\n' "$database" "$table" "$count"
  done
}

cmd_compare() {
  local before="${1:?first label}" after="${2:?second label}"
  require GATE_STATE_DIR
  local a="$GATE_STATE_DIR/$before-rows.txt" b="$GATE_STATE_DIR/$after-rows.txt"
  if ! diff <(comparable_rows "$a") <(comparable_rows "$b"); then
    fail "Row counts differ between $before and $after (see diff above)."
  fi
  diff <(grep '|rows|' "$a") <(grep '|rows|' "$b") || note 'Differences above are in volatile tables only (logged, not compared).'
  if ! diff <(grep '|ext|' "$a" | cut -d'|' -f1,3) <(grep '|ext|' "$b" | cut -d'|' -f1,3); then
    fail "The installed extensions differ between $before and $after."
  fi
  grep '|ext|' "$b"
  if grep '|invalid_index|' "$b"; then fail "Invalid indexes in snapshot $after."; fi
  if ! diff <(grep '^knn|' "$GATE_STATE_DIR/$before-knn.txt") <(grep '^knn|' "$GATE_STATE_DIR/$after-knn.txt"); then
    paste -d'\n' <(grep '^knnd|' "$GATE_STATE_DIR/$before-knn.txt") <(grep '^knnd|' "$GATE_STATE_DIR/$after-knn.txt")
    fail "Exact top-10 lists differ between $before and $after."
  fi
  note "Snapshots $before and $after match: $(comparable_rows "$b" | wc -l) tables compared, $KNN_LIST_COUNT KNN lists identical."
}

enqueue_once() {
  bash "$GATES_DIR/../enqueue-engine-upgrade.sh" \
    --psql "sudo docker exec -i $POSTGRES_CONTAINER psql -U $POSTGRES_USER -d $KLACKS_DATABASE" \
    --from "$GATE_FROM_MAJOR" --to "$GATE_TO_MAJOR" --ref "$GATE_ENGINE_REF" --sha "$GATE_ENGINE_SHA" \
    --sig "$(cat "$GATE_SIGNATURE_FILE")" --requested-by engine-upgrade-gate
}

cmd_enqueue() {
  require GATE_STATE_DIR GATE_FROM_MAJOR GATE_TO_MAJOR GATE_ENGINE_REF GATE_ENGINE_SHA GATE_SIGNATURE_FILE
  local id code=0
  id="$(enqueue_once)"
  echo "$id" > "$GATE_STATE_DIR/operation-id"
  note "Queued engine upgrade $id ($GATE_FROM_MAJOR -> $GATE_TO_MAJOR, $GATE_ENGINE_REF@$GATE_ENGINE_SHA)."
  enqueue_once >/dev/null 2>&1 || code=$?
  [ "$code" = "$EXIT_OPERATION_ACTIVE" ] || fail "A second enqueue while $id is active ended with exit code $code, expected $EXIT_OPERATION_ACTIVE."
  note "A second enqueue was refused while the first operation is active (exit code $EXIT_OPERATION_ACTIVE)."
}

row_state() {
  psql_query "$KLACKS_DATABASE" "SELECT status || '|' || coalesce(message, '') FROM update_history WHERE id = '$(cat "$GATE_STATE_DIR/operation-id")';" 2>/dev/null
}

cmd_wait_terminal() {
  local expected="${1:?expected statuses}" deadline=$(( $(date +%s) + UPGRADE_TIMEOUT_SECONDS )) state status
  require GATE_STATE_DIR
  while :; do
    state="$(row_state || true)"
    status="${state%%|*}"
    case "$status" in
      "$STATUS_SUCCEEDED"|"$STATUS_FAILED"|"$STATUS_ROLLED_BACK"|"$STATUS_ROLLBACK_FAILED") break ;;
    esac
    [ "$(date +%s)" -lt "$deadline" ] || fail "The engine upgrade did not finish within ${UPGRADE_TIMEOUT_SECONDS}s (last state '$state')."
    echo "$(date -u +%H:%M:%S) status '${status:-unreachable}'"
    sleep "$UPGRADE_POLL_SECONDS"
  done
  echo "$status" > "$GATE_STATE_DIR/final-status"
  note "Engine upgrade finished: $state"
  [[ ",$expected," == *",$status,"* ]] || fail "Status $status, expected one of $expected: ${state#*|}"
}

journal_phase() {
  sudo docker exec "$UPDATER_CONTAINER" cat "$JOURNAL_DIR/journal.json" 2>/dev/null | jq -r '.phase // empty' 2>/dev/null || true
}

cmd_kill_during_restore() {
  local deadline=$(( $(date +%s) + UPGRADE_TIMEOUT_SECONDS )) phase status
  require GATE_STATE_DIR
  while :; do
    phase="$(journal_phase)"
    case "$phase" in
      NewEngineActive|Restored|Verified)
        sudo docker kill "$UPDATER_CONTAINER" >/dev/null
        echo "$phase" > "$GATE_STATE_DIR/kill-phase"
        note "klacks-updater killed in phase $phase."
        break ;;
      ConfigurationCommitted|ApplicationHealthy|Committed|RolledBack|RollbackFailed)
        fail "The upgrade reached $phase before the gate could kill the updater (window missed; raise GATE_FILLER_ROWS)." ;;
    esac
    status="$(row_state || true)"
    case "${status%%|*}" in
      "$STATUS_SUCCEEDED"|"$STATUS_FAILED"|"$STATUS_ROLLED_BACK"|"$STATUS_ROLLBACK_FAILED")
        fail "The operation finished ($status) before the restore phase was observed." ;;
    esac
    [ "$(date +%s)" -lt "$deadline" ] || fail 'The restore phase was never reached.'
    sleep "$KILL_POLL_SECONDS"
  done
  # restart: unless-stopped may already have restarted it; docker start is idempotent.
  sudo docker start "$UPDATER_CONTAINER" >/dev/null
  note 'klacks-updater started again; its startup recovery has to revert the upgrade.'
}

assert_journal() {
  local expected_outcome="$1" id journal
  id="$(cat "$GATE_STATE_DIR/operation-id")"
  if sudo docker exec "$UPDATER_CONTAINER" test -e "$JOURNAL_DIR/journal.json"; then
    fail 'The engine upgrade journal is still open after the operation finished.'
  fi
  journal="$(sudo docker exec "$UPDATER_CONTAINER" cat "$JOURNAL_DIR/journal-$id.json")" || fail "No archived journal journal-$id.json."
  jq -c '{phase, outcome, fromMajor, toMajor, mode}' <<<"$journal"
  [ "$(jq -r '.outcome' <<<"$journal")" = "$expected_outcome" ] || fail "Journal outcome is $(jq -r '.outcome' <<<"$journal"), expected $expected_outcome."
}

cmd_assert_success() {
  require GATE_STATE_DIR GATE_FROM_MAJOR GATE_TO_MAJOR GATE_EXPECTED_VECTOR GATE_ENGINE_REF GATE_ENGINE_SHA
  local repository pinned data_directory old_version
  sync_sql
  psql_file "$KLACKS_DATABASE" assert-engine.sql -v "expected_major=$GATE_TO_MAJOR" -v "expected_vector=$GATE_EXPECTED_VECTOR"
  psql_file "$KLACKS_DATABASE" assert-hnsw.sql
  repository="${GATE_ENGINE_REF%:*}"
  pinned="$repository@$GATE_ENGINE_SHA"
  [ "$(env_get "$IMAGE_ENV")" = "$pinned" ] || fail ".env $IMAGE_ENV is '$(env_get "$IMAGE_ENV")', expected $pinned."
  [ "$(env_get "$SUBDIR_ENV")" = "pg$GATE_TO_MAJOR" ] || fail ".env $SUBDIR_ENV is '$(env_get "$SUBDIR_ENV")', expected pg$GATE_TO_MAJOR."
  [ "$(sudo docker inspect --format '{{.Config.Image}}' "$POSTGRES_CONTAINER")" = "$pinned" ] \
    || fail "klacks-postgres runs $(sudo docker inspect --format '{{.Config.Image}}' "$POSTGRES_CONTAINER"), expected the signed digest $pinned."
  data_directory="$(psql_query "$KLACKS_DATABASE" 'SHOW data_directory;' | tr -d '\r')"
  [ "$data_directory" = "$VOLUME_ROOT/pg$GATE_TO_MAJOR" ] || fail "data_directory is $data_directory."
  old_version="$(sudo docker exec "$POSTGRES_CONTAINER" cat "$VOLUME_ROOT/$LEGACY_SUBDIR/PG_VERSION" | tr -d '\r')"
  [ "$old_version" = "$GATE_FROM_MAJOR" ] || fail "The old cluster $LEGACY_SUBDIR has PG_VERSION '$old_version', expected $GATE_FROM_MAJOR (kept for rollback)."
  assert_journal Succeeded
  wait_healthy
  api_login
  note "Upgrade verified: PostgreSQL $GATE_TO_MAJOR from $pinned, old $GATE_FROM_MAJOR cluster kept."
}

cmd_assert_rollback() {
  require GATE_STATE_DIR GATE_FROM_MAJOR GATE_TO_MAJOR
  sync_sql
  psql_file "$KLACKS_DATABASE" assert-engine.sql -v "expected_major=$GATE_FROM_MAJOR" -v 'expected_vector='
  [ "$(env_get "$IMAGE_ENV")" = "$LEGACY_IMAGE" ] || fail ".env $IMAGE_ENV is '$(env_get "$IMAGE_ENV")' after the rollback, expected $LEGACY_IMAGE."
  [ "$(env_get "$SUBDIR_ENV")" = "$LEGACY_SUBDIR" ] || fail ".env $SUBDIR_ENV is '$(env_get "$SUBDIR_ENV")' after the rollback, expected $LEGACY_SUBDIR."
  [ "$(psql_query "$KLACKS_DATABASE" 'SHOW data_directory;' | tr -d '\r')" = "$VOLUME_ROOT/$LEGACY_SUBDIR" ] || fail 'PostgreSQL does not run from the old data folder.'
  if sudo docker exec "$POSTGRES_CONTAINER" test -e "$VOLUME_ROOT/pg$GATE_TO_MAJOR/PG_VERSION"; then
    fail "The failed target cluster pg$GATE_TO_MAJOR is still in the volume (install.sh refuses to run next to it)."
  fi
  assert_journal RolledBack
  wait_healthy
  api_login
  note "Rollback verified: PostgreSQL $GATE_FROM_MAJOR active, pin restored."
}

cmd_restart_stack() {
  require GATE_STATE_DIR
  local expected_major="${1:?expected major}"
  compose down
  compose up -d
  wait_healthy
  sync_sql
  psql_file "$KLACKS_DATABASE" assert-engine.sql -v "expected_major=$expected_major" -v 'expected_vector='
  api_login
  note "compose down + up -d: healthy again on PostgreSQL $expected_major."
}

cmd_installer_rerun() {
  require GATE_STATE_DIR
  local before image subdir
  before="$(container_id "$POSTGRES_CONTAINER")"
  image="$(env_get "$IMAGE_ENV")"
  subdir="$(env_get "$SUBDIR_ENV")"
  run_installer "$GATE_STATE_DIR/install-rerun.log"
  wait_healthy
  [ "$(container_id "$POSTGRES_CONTAINER")" = "$before" ] || fail 'Re-running install.sh recreated the postgres container.'
  if [ "$(env_get "$IMAGE_ENV")" != "$image" ] || [ "$(env_get "$SUBDIR_ENV")" != "$subdir" ]; then
    fail 'Re-running install.sh changed the engine pin.'
  fi
  note "install.sh re-run kept postgres and the pin ($image / $subdir)."
}

cmd_fresh_install() {
  require GATE_INSTALL_DIR GATE_REPO_DIR GATE_STATE_DIR GATE_UPDATER_SHA GATE_PUBLIC_KEY_FILE GATE_BASELINE_VERSION \
    GATE_TO_MAJOR GATE_EXPECTED_VECTOR GATE_ADMIN_EMAIL GATE_ADMIN_PASSWORD
  local zip data_directory image
  mkdir -p "$GATE_STATE_DIR"
  copy_candidate_bundle "$GATE_INSTALL_DIR"
  # The vendor update key ships only in released bundles; the API keeps verifying with it, the updater gets the CI key.
  zip="$GATE_STATE_DIR/klacks-onprem-$GATE_BASELINE_VERSION.zip"
  curl -fsSL --retry 3 -o "$zip" "$RELEASE_DOWNLOAD_BASE/v$GATE_BASELINE_VERSION/klacks-onprem.zip"
  unzip -p "$zip" onprem/update-public-key.pem | sudo tee "$GATE_INSTALL_DIR/update-public-key.pem" >/dev/null
  printf '%s=%s\nUPDATE_AUTO_ENABLED=false\nUPDATE_NOTIFY_ONLY=true\n' "$UPDATER_TAG_ENV" "sha-$GATE_UPDATER_SHA" \
    | sudo tee "$GATE_INSTALL_DIR/.env" >/dev/null
  write_override
  run_installer "$GATE_STATE_DIR/install-fresh.log"
  wait_healthy
  [ -z "$(env_get "$IMAGE_ENV")" ] || fail "A fresh install pinned $IMAGE_ENV=$(env_get "$IMAGE_ENV")."
  image="$(sudo docker inspect --format '{{.Config.Image}}' "$POSTGRES_CONTAINER")"
  note "klacks-postgres image: $image"
  sync_sql
  # assert-engine.sql also requires the gate's HNSW index, so the gate table comes first.
  psql_file "$KLACKS_DATABASE" seed-gate-vectors.sql
  psql_file "$KLACKS_DATABASE" assert-engine.sql -v "expected_major=$GATE_TO_MAJOR" -v "expected_vector=$GATE_EXPECTED_VECTOR"
  psql_file "$KLACKS_DATABASE" assert-hnsw.sql
  data_directory="$(psql_query "$KLACKS_DATABASE" 'SHOW data_directory;' | tr -d '\r')"
  [ "$data_directory" = "$VOLUME_ROOT/$LEGACY_SUBDIR" ] || fail "A fresh install keeps its data in $data_directory, expected $VOLUME_ROOT/$LEGACY_SUBDIR."
  first_start
  disable_auto_update
  wait_capability Docker
  cmd_restart_stack "$GATE_TO_MAJOR"
  note "Fresh install verified: PostgreSQL $GATE_TO_MAJOR from the start ($image)."
}

cmd_diagnostics() {
  local dir="${1:?diagnostics dir}"
  set +e
  mkdir -p "$dir"
  docker ps -a --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}' > "$dir/docker-ps.txt" 2>&1
  [ -d "${GATE_INSTALL_DIR:-}" ] && compose logs --no-color --timestamps > "$dir/compose.log" 2>&1
  docker exec "$UPDATER_CONTAINER" sh -c "ls -la $JOURNAL_DIR; for f in $JOURNAL_DIR/journal*.json; do echo \"== \$f\"; cat \"\$f\"; done" \
    > "$dir/journal.txt" 2>&1
  psql_query "$KLACKS_DATABASE" "SELECT id, operation_type, status, from_version, target_version, artifact_ref, artifact_sha256, requested_at, started_at, completed_at, message FROM update_history ORDER BY requested_at;" \
    > "$dir/update-history.txt" 2>&1
  psql_query "$KLACKS_DATABASE" "SELECT value FROM settings WHERE type = 'UPDATER_CAPABILITIES';" > "$dir/capabilities.txt" 2>&1
  [ -d "${GATE_STATE_DIR:-}" ] && cp "$GATE_STATE_DIR"/*.txt "$GATE_STATE_DIR"/*.log "$dir/" 2>/dev/null
  df -h > "$dir/disk.txt" 2>&1
  local secret
  for secret in "${GATE_ADMIN_PASSWORD:-}" "$(env_get KLACKS_SETUP_CODE)" "$(env_get POSTGRES_PASSWORD)" "$(env_get JWT_SECRET)"; do
    [ -n "$secret" ] || continue
    SECRET_VALUE="$secret" find "$dir" -type f -exec perl -pi -e 's/\Q$ENV{SECRET_VALUE}\E/***/g' {} +
  done
  return 0
}

main() {
  local command="${1:-}"
  [ $# -gt 0 ] && shift
  case "$command" in
    install-baseline) cmd_install_baseline ;;
    apply-candidate) cmd_apply_candidate ;;
    seed) cmd_seed ;;
    calibrate) cmd_calibrate ;;
    snapshot) cmd_snapshot "$@" ;;
    compare) cmd_compare "$@" ;;
    enqueue) cmd_enqueue ;;
    kill-during-restore) cmd_kill_during_restore ;;
    wait-terminal) cmd_wait_terminal "$@" ;;
    assert-success) cmd_assert_success ;;
    assert-rollback) cmd_assert_rollback ;;
    restart-stack) cmd_restart_stack "$@" ;;
    installer-rerun) cmd_installer_rerun ;;
    fresh-install) cmd_fresh_install ;;
    diagnostics) cmd_diagnostics "$@" ;;
    *) echo "Unknown subcommand '$command'; the subcommands are listed in the header of $0." >&2; exit 2 ;;
  esac
}

main "$@"
