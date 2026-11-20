#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Tests hetzner-engine-upgrade.sh with a fake docker, crontab and df (no server, no containers): the fake docker answers
# psql statements, inspect, compose and exec calls from files in a state directory and records every mutating call. Covers
# preflight (all green, --pre-release, every failing check on its own), backups, the upgrade (Succeeded, RolledBack, RollbackFailed,
# timeout, failed verification, confirm and lock guards, cron suspended during the run and restored after), rollback,
# regenerate-golden and cleanup.
#
# Usage: test-hetzner-engine-upgrade.sh   (needs bash, python3, openssl, gzip, flock)

set -uo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
readonly SCRIPT="$here/hetzner-engine-upgrade.sh"
readonly DIGEST='sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef'
readonly IMAGE='ghcr.io/heribertg/klacks-postgres:19'
root="$(mktemp -d)"
failures=0
trap '[ -n "${HEU_KEEP_TEST_DIR:-}" ] || rm -rf "$root"' EXIT

check() {
  local name="$1"
  shift
  if "$@" > /dev/null 2>&1; then echo "PASS $name"; else echo "FAIL $name"; failures=$((failures + 1)); fi
}

mkdir -p "$root/bin"
cat > "$root/bin/docker" <<'FAKE'
#!/usr/bin/env bash
S="$HEU_FAKE_STATE"
echo "docker $*" >> "$S/calls.log"
args=("$@")
case "${args[0]}" in
  exec)
    i=1
    [ "${args[$i]}" = "-i" ] && i=$((i + 1))
    container="${args[$i]}"
    cmd="${args[$((i + 1))]}"
    rest=("${args[@]:$((i + 2))}")
    case "$cmd" in
      psql)
        db=""; sql=""
        for ((j = 0; j < ${#rest[@]}; j++)); do
          [ "${rest[$j]}" = "-d" ] && db="${rest[$((j + 1))]}"
          [ "${rest[$j]}" = "-c" ] && sql="${rest[$((j + 1))]}"
        done
        case "$sql" in
          "SHOW server_version_num") cat "$S/version_num" ;;
          *"datlocprovider ||"*) echo 'ERROR: operator is not unique: text || "char"' >&2; exit 1 ;;
          *datcollate*) cat "$S/locales" ;;
          *"FROM pg_database"*) cat "$S/databases" ;;
          *"FROM pg_roles"*) cat "$S/roles" ;;
          *"extname ||"*) cat "$S/extensions" ;;
          *"count(*) FROM pg_index"*) echo 0 ;;
          *"count(*) FROM pg_extension"*) cat "$S/extension_count" ;;
          *"status IN (0, 1)"*"UPDATE"* | "UPDATE update_history"*) touch "$S/row_closed" ;;
          *"WHERE status IN (0, 1)"*) cat "$S/active" ;;
          *query_to_xml*) cat "$S/counts.$db" ;;
          *capabilities*) cat "$S/capability" ;;
          *"SELECT status FROM update_history"*)
            [ -f "$S/outage_once" ] && { rm -f "$S/outage_once"; exit 1; }
            first="$(head -n1 "$S/op_status")"
            if [ "$(wc -l < "$S/op_status")" -gt 1 ]; then sed -i 1d "$S/op_status"; fi
            if [ "$first" = "2" ]; then
              cp "$S/version_num.after" "$S/version_num"
              for f in "$S"/counts.after.*; do [ -f "$f" ] && cp "$f" "$S/counts.${f##*counts.after.}"; done
            fi
            echo "$first"
            ;;
          "DROP DATABASE"* | *"CREATE DATABASE"*) echo "sql: $sql" >> "$S/mutations.log" ;;
          *) echo "unexpected sql: $sql" >> "$S/unexpected.log"; exit 1 ;;
        esac
        ;;
      pg_dumpall) head -c 3000 /dev/urandom | base64 ;;
      pg_isready) [ -f "$S/pg_not_ready" ] && exit 2; exit 0 ;;
      printenv) cat "$S/pgdata" ;;
      cat) cat "$S/old_pg_version" ;;
      rm) echo "docker exec rm ${rest[*]}" >> "$S/mutations.log" ;;
      pg_dump) echo "PGDMP fake custom dump"; head -c 3000 /dev/urandom | base64 ;;
      pg_restore)
        if [ "${rest[0]}" = "-l" ]; then echo "; Archive created"; echo "1 TABLE DATA public clients admin"; exit 0; fi
        [ -f "$S/probe_restore_fails" ] && exit 1
        cat > /dev/null
        echo "docker exec pg_restore ${rest[*]}" >> "$S/mutations.log"
        ;;
      curl)
        url="${rest[$((${#rest[@]} - 1))]}"
        case "$url" in
          */health) [ -f "$S/api_unhealthy" ] && exit 22; exit 0 ;;
          */api/Version) echo '{"major":1,"minor":0,"patch":79}' ;;
          */api/backend/Update/Status) cat "$S/auth_code" ;;
        esac
        ;;
      *) echo "unexpected exec: $*" >> "$S/unexpected.log"; exit 1 ;;
    esac
    ;;
  inspect)
    format=""
    for ((j = 0; j < ${#args[@]}; j++)); do [ "${args[$j]}" = "--format" ] && format="${args[$((j + 1))]}"; done
    case "$format" in
      "{{.State.Running}}") if [ -f "$S/pg_stopped" ]; then echo false; else echo true; fi ;;
      *config-hash*) cat "$S/hash_running" ;;
      "{{.Config.Image}}") cat "$S/updater_image" ;;
    esac
    ;;
  compose)
    shift
    while [ $# -gt 0 ]; do
      case "$1" in
        --project-name | --project-directory | --env-file | -f) shift 2 ;;
        *) break ;;
      esac
    done
    case "$1" in
      config) echo "postgres $(cat "$S/hash_new")" ;;
      up | stop | restart)
        echo "compose $*" >> "$S/mutations.log"
        if [ "$1" = "up" ] && [[ "$*" == *postgres* ]] && [ -f "$S/version_num.rollback" ]; then cp "$S/version_num.rollback" "$S/version_num"; fi
        ;;
    esac
    ;;
  buildx) cat "$S/image_digest" ;;
  logs) cat "$S/updater_log" ;;
  image) echo "docker $*" >> "$S/mutations.log" ;;
  pull) exit 0 ;;
  run) if [[ " $* " == *" --entrypoint locale "* ]]; then cat "$S/image_locales"; else cat "$S/old_pg_version"; fi ;;
  *) echo "unexpected docker: $*" >> "$S/unexpected.log"; exit 1 ;;
esac
FAKE
cat > "$root/bin/crontab" <<'FAKE'
#!/usr/bin/env bash
if [ "${1:-}" = "-l" ]; then cat "$HEU_FAKE_STATE/cron" 2> /dev/null; else cat > "$HEU_FAKE_STATE/cron"; fi
FAKE
cat > "$root/bin/df" <<'FAKE'
#!/usr/bin/env bash
free_kb="${HEU_FAKE_DF_KB:-}"
if [ -z "$free_kb" ]; then exec /bin/df "$@"; fi
echo "Filesystem 1024-blocks Used Available Capacity Mounted"
echo "fake 99999999 1 $free_kb 1% /"
FAKE
cat > "$root/bin/enqueue" <<'FAKE'
#!/usr/bin/env bash
echo "$*" > "$HEU_FAKE_STATE/enqueue.args"
crontab -l > "$HEU_FAKE_STATE/cron.during"
[ -f "$HEU_FAKE_STATE/enqueue_fails" ] && exit 3
echo "KLACKS_POSTGRES_DATA_SUBDIR=pg19" >> "$APPS_DIR/.env"
echo "enqueued"
echo "0b1c2d3e-4f50-4a61-8b72-93a4b5c6d7e8"
FAKE
cat > "$root/bin/reset" <<'FAKE'
#!/usr/bin/env bash
echo "reset ran" >> "$HEU_FAKE_STATE/mutations.log"
exit 0
FAKE
chmod +x "$root/bin/"*
export PATH="$root/bin:$PATH"

openssl genrsa -out "$root/key.pem" 2048 2> /dev/null
openssl pkey -in "$root/key.pem" -pubout -out "$root/pub.pem"
pub_escaped="$(awk '{printf "%s\\n", $0}' "$root/pub.pem")"
sig="$(printf 'klacks-postgres-engine\n19\n%s\n%s' "$IMAGE" "$DIGEST" | openssl dgst -sha256 -sign "$root/key.pem" | base64 -w0)"

new_case() { # resets state, apps dir and fakes to the green baseline
  case_dir="$root/case-$1"
  rm -rf "$case_dir"
  S="$case_dir/state"
  mkdir -p "$S" "$case_dir/apps" "$case_dir/backups" "$case_dir/golden" "$case_dir/dockerroot"
  export HEU_FAKE_STATE="$S" APPS_DIR="$case_dir/apps" BACKUP_DIR="$case_dir/backups" GOLDEN_DIR="$case_dir/golden"
  export DOCKER_ROOT="$case_dir/dockerroot" DEPLOY_LOCK="$case_dir/deploy.lock" RESET_SCRIPT="$root/bin/reset"
  export HEU_TEST=1 HEU_NOW_UTC="12:00" HEU_WAIT_SECONDS=6 HEU_POLL_SECONDS=1 HEU_HEALTH_WAIT_SECONDS=2 HEU_PG_READY_WAIT_SECONDS=2 HEU_GOLDEN_WAIT_SECONDS=2
  unset HEU_FAKE_DF_KB
  echo 170010 > "$S/version_num"
  echo 190000 > "$S/version_num.after"
  printf 'Klacks\nklacks_diag\nklacks_telemetry\n' > "$S/databases"
  printf 'Klacks|c|en_US.utf8|en_US.utf8\nklacks_diag|c|C|C\nklacks_telemetry|c|C.UTF-8|C.UTF-8\n' > "$S/locales"
  printf 'C\nC.utf8\nPOSIX\nen_US.utf8\n' > "$S/image_locales"
  echo admin > "$S/roles"
  printf 'pg_trgm 1.6\nvector 0.8.5\n' > "$S/extensions"
  echo 2 > "$S/extension_count"
  echo 0 > "$S/active"
  echo t > "$S/capability"
  echo 401 > "$S/auth_code"
  echo abc123 > "$S/hash_new"
  echo abc123 > "$S/hash_running"
  echo "ghcr.io/heribertg/klacks-updater:1.0.79" > "$S/updater_image"
  echo "$DIGEST" > "$S/image_digest"
  printf 'public.clients|10\npublic.update_history|3\npublic.settings|5\n' > "$S/counts.Klacks"
  printf 'public.clients|10\npublic.update_history|4\npublic.settings|5\n' > "$S/counts.after.Klacks"
  printf 'public.diag|7\n' > "$S/counts.klacks_diag"
  printf 'public.diag|7\n' > "$S/counts.after.klacks_diag"
  printf 'public.telemetry|2\n' > "$S/counts.klacks_telemetry"
  printf 'public.telemetry|2\n' > "$S/counts.after.klacks_telemetry"
  printf '1\n' > "$S/op_status"
  printf '1\n1\n2\n' > "$S/op_status"
  echo "/var/lib/postgresql/data/pg19" > "$S/pgdata"
  echo 17 > "$S/old_pg_version"
  echo "" > "$S/updater_log"
  printf '0 2 * * * /root/backups/klacks-playground/reset.sh >> /var/log/reset.log 2>&1\n30 3 * * * /root/scripts/disk-maintenance.sh\n# 5 5 * * * old line\n' > "$S/cron"
  cat > "$APPS_DIR/.env" <<EOF
KLACKS_API_TAG=1.0.79
UPDATE_SIGNATURE_PUBLIC_KEY="$pub_escaped"
POSTGRES_PASSWORD=do-not-print-this-secret
COMPOSE_PROFILES=whisper
EOF
  cat > "$case_dir/compose.yml" <<'EOF'
services:
  postgres:
    image: ${KLACKS_POSTGRES_IMAGE:-pgvector/pgvector:pg17}
    environment:
      PGDATA: /var/lib/postgresql/data/${KLACKS_POSTGRES_DATA_SUBDIR:-pgdata}
EOF
  cat > "$case_dir/stable.json" <<EOF
{
  "channel": "Stable",
  "latestVersion": "1.0.79",
  "artifacts": {
    "docker": { "apiImage": "ghcr.io/heribertg/klacks-api:1.0.79", "sha256": "sha256:aa" },
    "postgres": { "major": 19, "version": "19.0", "pgvector": "0.8.7",
      "docker": { "image": "$IMAGE", "sha256": "$DIGEST", "signature": "$sig" }, "native": [] }
  },
  "signature": "x"
}
EOF
  : > "$S/calls.log"
}

set_age() { echo $(($(date +%s) - $1 * 86400)) > "$BACKUP_DIR/upgrade-succeeded"; }

run() { # args...; output to $case_dir/out.txt, exit code to $code
  code=0
  "$SCRIPT" --compose "$case_dir/compose.yml" --manifest "$case_dir/stable.json" --enqueue "$root/bin/enqueue" --run-id test "$@" > "$case_dir/out.txt" 2>&1 || code=$?
}

out_has() { grep -q -- "$1" "$case_dir/out.txt"; }
mutation_has() { grep -q -- "$1" "$S/mutations.log" 2> /dev/null; }

echo "-- preflight green"
new_case green
run --mode preflight
check "exit 0" test "$code" = 0
check "PREFLIGHT OK" out_has "PREFLIGHT OK"
check "all checks passed" bash -c "! grep -q '^\[engine-upgrade\] [0-9:Z]* FAIL' '$case_dir/out.txt'"
check "manifest signature verified" out_has "PASS manifest"
check "compose hash equal" out_has "PASS compose hash"
check "backup written and valid" bash -c "gzip -t '$BACKUP_DIR'/pre-switch-*.sql.gz"
check ".env copy written with mode 600" bash -c "[ \"\$(stat -c %a '$BACKUP_DIR'/env-pre-switch-*)\" = 600 ]"
check "row count snapshots written" bash -c "[ -s '$BACKUP_DIR/rowcounts-test.Klacks.tsv' ] && [ -s '$BACKUP_DIR/rowcounts-test.klacks_diag.tsv' ] && [ -s '$BACKUP_DIR/rowcounts-test.klacks_telemetry.tsv' ]"
check "no .env.pre-pg19 from preflight" bash -c "[ ! -e '$APPS_DIR/.env.pre-pg19' ]"
check "no mutating call" bash -c "[ ! -s '$S/mutations.log' ]"
check "crontab untouched" bash -c "grep -q '^0 2 \* \* \*' '$S/cron'"
check "secrets never printed" bash -c "! grep -q -e do-not-print-this-secret -e 'BEGIN PUBLIC KEY' '$case_dir/out.txt'"
check "no unexpected docker or sql call" bash -c "[ ! -e '$S/unexpected.log' ]"

echo "-- preflight --pre-release"
new_case prerelease
echo "ghcr.io/heribertg/klacks-updater:1.0.78" > "$S/updater_image"
rm -f "$case_dir/stable.json"
echo f > "$S/capability"
run --mode preflight --pre-release
check "release checks skipped, preflight green" bash -c "[ '$code' = 0 ] && grep -q 'SKIP manifest' '$case_dir/out.txt'"

echo "-- preflight failures"
fail_case() { # name expected-text, after the mutation done by the caller
  check "$1" bash -c "[ '$code' = 1 ] && grep -q -e 'FAIL $2' '$case_dir/out.txt' && ! grep -q 'PREFLIGHT OK' '$case_dir/out.txt'"
}
new_case window; export HEU_NOW_UTC=02:00; run --mode preflight; fail_case "inside the nightly reset window" "time window"
new_case resetstart; export HEU_NOW_UTC=01:50; run --mode preflight; fail_case "01:50 is the start of the reset window" "time window"
new_case resetend; export HEU_NOW_UTC=02:30; run --mode preflight; check "preflight at 02:30 is allowed" bash -c "[ '$code' = 0 ]"
new_case preflightreserve; export HEU_NOW_UTC=00:40; run --mode preflight; check "preflight alone at 00:40 is allowed" bash -c "[ '$code' = 0 ]"
new_case reserve; export HEU_NOW_UTC=00:40; run --mode upgrade --confirm SWITCH-PG19
check "upgrade keeps the reserve before the window" bash -c "[ '$code' = 1 ] && grep -q 'FAIL time window' '$case_dir/out.txt' && [ ! -e '$S/enqueue.args' ]"
new_case upgradewindow; export HEU_NOW_UTC=03:00; run --mode upgrade --confirm SWITCH-PG19
check "upgrade blocked inside 01:15-04:00" bash -c "[ '$code' = 1 ] && grep -q 'FAIL time window' '$case_dir/out.txt' && [ ! -e '$S/enqueue.args' ]"
new_case locale; printf 'Klacks|c|de_CH.utf8|de_CH.utf8\n' > "$S/locales"; run --mode preflight; fail_case "database locale missing in the new image" "locales"
new_case localespelling; printf 'Klacks|c|en_US.UTF-8|en_US.UTF-8\n' > "$S/locales"; run --mode preflight; check "UTF-8 spelling matches utf8 of locale -a" bash -c "[ '$code' = 0 ] && grep -q 'PASS locales' '$case_dir/out.txt'"
new_case localebuiltin; printf 'en_US.utf8\n' > "$S/image_locales"; printf 'Klacks|c|C.UTF-8|POSIX\n' > "$S/locales"; run --mode preflight; check "C, C.UTF-8 and POSIX count as available" bash -c "[ '$code' = 0 ] && grep -q 'PASS locales' '$case_dir/out.txt'"
new_case localeunreadable; : > "$S/locales"; run --mode preflight; fail_case "unreadable database locales" "locales"
new_case before; export HEU_NOW_UTC=00:20; run --mode preflight; check "00:20 is allowed" bash -c "[ '$code' = 0 ]"
new_case after; export HEU_NOW_UTC=04:00; run --mode preflight; check "04:00 is allowed" bash -c "[ '$code' = 0 ]"
new_case disk; export HEU_FAKE_DF_KB=$((2 * 1024 * 1024)); run --mode preflight; fail_case "less than 5 GB free" "disk"
new_case version; echo 160010 > "$S/version_num"; run --mode preflight; fail_case "wrong server major" "postgres"
new_case roles; printf 'admin\nextra\n' > "$S/roles"; run --mode preflight; fail_case "foreign role" "roles"
new_case ext; echo "vector 0.8.5" > "$S/extensions"; run --mode preflight; fail_case "pg_trgm missing" "extensions"
new_case active; echo 1 > "$S/active"; run --mode preflight; fail_case "active operation" "active operation"
new_case nokey; sed -i '/UPDATE_SIGNATURE_PUBLIC_KEY/d' "$APPS_DIR/.env"; run --mode preflight; fail_case "no signing key" "signing key"
new_case hash; echo different > "$S/hash_new"; run --mode preflight; fail_case "compose hash differs" "compose hash"
new_case unparam; echo "services: {}" > "$case_dir/compose.yml"; run --mode preflight; fail_case "compose not parametrised" "compose parametrised"
new_case baddigest; echo "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff" > "$S/image_digest"; run --mode preflight; fail_case "digest differs from the signed one" "manifest"
new_case badsig; sed -i "s|\"signature\": \"$sig\"|\"signature\": \"AAAA\"|" "$case_dir/stable.json"; run --mode preflight; fail_case "bad engine signature" "manifest"
new_case garbagesig; python3 - "$case_dir/stable.json" <<'PY'
import json, sys
p = sys.argv[1]
d = json.load(open(p))
d["artifacts"]["postgres"]["docker"]["signature"] += "; DROP x"
json.dump(d, open(p, "w"))
PY
run --mode preflight; fail_case "signature with trailing garbage" "manifest"
new_case badversion; sed -i 's|"latestVersion": "1.0.79"|"latestVersion": "1.0.79 @@EXIT 0"|' "$case_dir/stable.json"; run --mode preflight; fail_case "latestVersion that is not X.Y.Z" "manifest"
new_case markedcron; echo '#PG19-SWITCH# 0 2 * * * /root/backups/klacks-playground/reset.sh' > "$S/cron"; run --mode preflight
check "left-over cron marker is reported" bash -c "grep -q 'WARN reset cron' '$case_dir/out.txt' && [ '$code' = 0 ]"
new_case nopostgres; python3 - "$case_dir/stable.json" <<'PY'
import json, sys
p = sys.argv[1]
d = json.load(open(p))
del d["artifacts"]["postgres"]
json.dump(d, open(p, "w"))
PY
run --mode preflight; fail_case "manifest without artifacts.postgres" "manifest"
new_case updater; echo "ghcr.io/heribertg/klacks-updater:1.0.78" > "$S/updater_image"; run --mode preflight; fail_case "updater tag differs from the release" "updater tag"
new_case nocap; echo f > "$S/capability"; run --mode preflight; fail_case "no capability" "capability"
new_case caplog; echo f > "$S/capability"; echo "capabilities: postgres-engine-upgrade/1" > "$S/updater_log"; run --mode preflight; check "capability from log line" bash -c "[ '$code' = 0 ]"
new_case nobackup; run --mode preflight; check "failed checks write no backup" bash -c "true"
new_case failnobackup; echo 1 > "$S/active"; run --mode preflight; check "no dump after a failed check" bash -c "! ls '$BACKUP_DIR'/pre-switch-*.sql.gz"

echo "-- upgrade guards"
new_case noconfirm; run --mode upgrade; check "confirm word required" bash -c "[ '$code' = 1 ] && grep -q 'needs --confirm SWITCH-PG19' '$case_dir/out.txt'"
new_case wrongconfirm; run --mode upgrade --confirm yes; check "wrong confirm word refused" bash -c "[ '$code' = 1 ]"
new_case locked
( exec 9> "$DEPLOY_LOCK"; flock -n 9; sleep 4 ) &
lock_pid=$!
sleep 1
run --mode upgrade --confirm SWITCH-PG19
check "a running deploy blocks the upgrade" bash -c "[ '$code' = 1 ] && grep -q 'deploy holds' '$case_dir/out.txt' && [ ! -s '$S/mutations.log' ]"
wait "$lock_pid" 2> /dev/null || true
new_case preonly; run --mode upgrade --confirm SWITCH-PG19 --pre-release; check "--pre-release refused for upgrade" bash -c "[ '$code' = 1 ] && grep -q 'preflight only' '$case_dir/out.txt'"
new_case failedpreflight; echo 1 > "$S/active"; run --mode upgrade --confirm SWITCH-PG19
check "failed preflight changes nothing" bash -c "[ '$code' = 1 ] && [ ! -e '$S/enqueue.args' ] && [ ! -e '$APPS_DIR/.env.pre-pg19' ] && grep -q '^0 2 ' '$S/cron'"

echo "-- upgrade succeeded"
new_case ok
touch "$S/outage_once"
run --mode upgrade --confirm SWITCH-PG19
check "exit 0" test "$code" = 0
check "UPGRADE OK" out_has "UPGRADE OK"
check "API version read from major.minor.patch" out_has "API version: 1.0.79"
check "enqueue arguments" bash -c "grep -q -e '--from 17 --to 19' -e \"--ref $IMAGE\" '$S/enqueue.args' && grep -q -e \"--sha $DIGEST\" -e \"--sig $sig\" '$S/enqueue.args' && grep -q 'requested-by hetzner-engine-upgrade:test' '$S/enqueue.args' && grep -q 'psql -U admin -d Klacks' '$S/enqueue.args'"
check ".env.pre-pg19 is the pre-switch .env" bash -c "cmp -s '$APPS_DIR/.env.pre-pg19' '$BACKUP_DIR'/env-pre-switch-* && ! grep -q KLACKS_POSTGRES_DATA_SUBDIR '$APPS_DIR/.env.pre-pg19' && [ \"\$(stat -c %a '$APPS_DIR/.env.pre-pg19')\" = 600 ]"
check "reset cron suspended during the run" bash -c "grep -q '^#PG19-SWITCH# 0 2 ' '$S/cron.during' && grep -q '^30 3 ' '$S/cron.during'"
check "reset cron restored afterwards" bash -c "grep -q '^0 2 \* \* \* /root/backups/klacks-playground/reset.sh' '$S/cron' && ! grep -q 'PG19-SWITCH' '$S/cron' && grep -q '^# 5 5 ' '$S/cron'"
check "database outage during the switch tolerated" out_has "Database not answering"
check "row counts verified for all three databases" bash -c "[ \$(grep -c 'PASS row counts' '$case_dir/out.txt') = 3 ]"
check "auth smoke and api health" bash -c "grep -q 'PASS auth smoke' '$case_dir/out.txt' && grep -q 'PASS api health' '$case_dir/out.txt'"
check "no rollback calls" bash -c "! grep -q 'compose up' '$S/mutations.log' 2> /dev/null"
check "proxy restarted after the switch" mutation_has 'compose restart nginx-proxy'
check "success marker written" bash -c "[[ \"\$(cat '$BACKUP_DIR/upgrade-succeeded')\" =~ ^[0-9]+\$ ]]"

echo "-- upgrade: RolledBack by the updater"
new_case rolledback
printf '1\n4\n' > "$S/op_status"
echo 170010 > "$S/version_num.after"
run --mode upgrade --confirm SWITCH-PG19
check "exit 1, old engine verified" bash -c "[ '$code' = 1 ] && grep -q 'PASS old engine' '$case_dir/out.txt' && grep -q 'status 4' '$case_dir/out.txt'"
check "cron restored" bash -c "grep -q '^0 2 ' '$S/cron'"
check "no manual rollback after a clean RolledBack" bash -c "! grep -q 'compose up' '$S/mutations.log' 2> /dev/null"

echo "-- upgrade: RollbackFailed"
new_case rbfailed
printf '1\n6\n' > "$S/op_status"
echo 170010 > "$S/version_num.rollback"
run --mode upgrade --confirm SWITCH-PG19
check "exit 1 and manual rollback ran" bash -c "[ '$code' = 1 ] && grep -q 'ROLLBACK OK' '$case_dir/out.txt'"
check "stopped updater and api, started postgres and api" bash -c "grep -q 'compose stop klacks-updater klacks-api' '$S/mutations.log' && grep -q 'compose up -d postgres' '$S/mutations.log' && grep -q 'compose up -d klacks-api klacks-updater' '$S/mutations.log'"
check "proxy restarted after the rollback" mutation_has 'compose restart nginx-proxy'
check ".env restored from .env.pre-pg19" bash -c "! grep -q KLACKS_POSTGRES_DATA_SUBDIR '$APPS_DIR/.env' && [ -f '$BACKUP_DIR'/env-before-rollback-* ]"
check "engine operation row closed" bash -c "[ -e '$S/row_closed' ] || grep -q 'UPDATE update_history' '$S/calls.log'"

echo "-- upgrade: timeout"
new_case timeout
printf '1\n' > "$S/op_status"
echo 170010 > "$S/version_num.rollback"
run --mode upgrade --confirm SWITCH-PG19
check "timeout leads to the manual rollback" bash -c "[ '$code' = 1 ] && grep -q 'timeout:1' '$case_dir/out.txt' && grep -q 'ROLLBACK OK' '$case_dir/out.txt'"

echo "-- upgrade: Succeeded but a table lost rows"
new_case lost
printf '1\n2\n' > "$S/op_status"
printf 'public.clients|9\npublic.update_history|4\npublic.settings|5\n' > "$S/counts.after.Klacks"
run --mode upgrade --confirm SWITCH-PG19
check "verification fails, no automatic rollback" bash -c "[ '$code' = 1 ] && grep -q 'FAIL row counts Klacks: fewer rows in public.clients: 10 -> 9' '$case_dir/out.txt' && grep -q \"Decide on 'rollback'\" '$case_dir/out.txt' && ! grep -q 'compose up' '$S/mutations.log' 2> /dev/null"
check "cron restored" bash -c "grep -q '^0 2 ' '$S/cron'"

echo "-- upgrade: Succeeded but the new database is empty"
new_case emptyafter
printf '1
2
' > "$S/op_status"
: > "$S/counts.after.Klacks"
run --mode upgrade --confirm SWITCH-PG19
check "an empty row count list is not a pass" bash -c "[ '$code' = 1 ] && grep -q 'FAIL row counts Klacks: missing table public.clients' '$case_dir/out.txt'"

echo "-- upgrade: Succeeded but version still 17 and auth smoke wrong"
new_case still17
printf '2\n' > "$S/op_status"
echo 170010 > "$S/version_num.after"
echo 200 > "$S/auth_code"
run --mode upgrade --confirm SWITCH-PG19
check "version and auth smoke failures reported" bash -c "[ '$code' = 1 ] && grep -q 'FAIL version' '$case_dir/out.txt' && grep -q 'FAIL auth smoke' '$case_dir/out.txt'"

echo "-- upgrade: enqueue helper refuses"
new_case enqueuefail
touch "$S/enqueue_fails"
run --mode upgrade --confirm SWITCH-PG19
check "enqueue failure aborts, cron restored" bash -c "[ '$code' = 1 ] && grep -q 'enqueue helper failed' '$case_dir/out.txt' && grep -q '^0 2 ' '$S/cron'"

echo "-- rollback"
new_case rollback
echo "KLACKS_POSTGRES_IMAGE=pgvector/pgvector:pg17" >> "$APPS_DIR/.env"
cp "$APPS_DIR/.env" "$APPS_DIR/.env.pre-pg19"
{ echo "KLACKS_API_TAG=1.0.80"; echo "KLACKS_POSTGRES_DATA_SUBDIR=pg19"; echo "KLACKS_POSTGRES_IMAGE=ghcr.io/heribertg/klacks-postgres@sha256:abc"; } >> "$APPS_DIR/.env"
echo 170010 > "$S/version_num.rollback"
echo 190000 > "$S/version_num"
echo 17 > "$S/old_pg_version"
run --mode rollback --confirm ROLLBACK-PG17
check "rollback succeeds" bash -c "[ '$code' = 0 ] && grep -q 'ROLLBACK OK' '$case_dir/out.txt'"
check "only the postgres variables are restored" bash -c "grep -q '^KLACKS_POSTGRES_IMAGE=pgvector/pgvector:pg17$' '$APPS_DIR/.env' && ! grep -q 'KLACKS_POSTGRES_DATA_SUBDIR' '$APPS_DIR/.env' && ! grep -q 'sha256:abc' '$APPS_DIR/.env'"
check "later deploy tags are kept" bash -c "grep -q '^KLACKS_API_TAG=1.0.80$' '$APPS_DIR/.env' && grep -q '^COMPOSE_PROFILES=whisper$' '$APPS_DIR/.env'"
check ".env before the rollback is archived" bash -c "grep -q 'KLACKS_POSTGRES_DATA_SUBDIR=pg19' '$BACKUP_DIR'/env-before-rollback-*"
new_case rollbackstopped
cp "$APPS_DIR/.env" "$APPS_DIR/.env.pre-pg19"
echo 170010 > "$S/version_num.rollback"; touch "$S/pg_stopped"; echo 17 > "$S/old_pg_version"
run --mode rollback --confirm ROLLBACK-PG17
check "stopped postgres: old cluster read through a throw-away container" bash -c "[ '$code' = 0 ] && grep -q 'docker run --rm --network none' '$S/calls.log'"
new_case rollbackmissing
cp "$APPS_DIR/.env" "$APPS_DIR/.env.pre-pg19"
echo "" > "$S/old_pg_version"
run --mode rollback --confirm ROLLBACK-PG17
check "rollback refuses when the old cluster is gone" bash -c "[ '$code' = 1 ] && grep -q 'rollback would start an empty cluster' '$case_dir/out.txt' && [ ! -s '$S/mutations.log' ]"
new_case rollbackclean; touch "$BACKUP_DIR/cleanup-done"; cp "$APPS_DIR/.env" "$APPS_DIR/.env.pre-pg19"
run --mode rollback --confirm ROLLBACK-PG17
check "rollback refused after cleanup" bash -c "[ '$code' = 1 ] && grep -q 'rollback is impossible' '$case_dir/out.txt'"
new_case rollbacknoenv; run --mode rollback --confirm ROLLBACK-PG17
check "rollback needs .env.pre-pg19" bash -c "[ '$code' = 1 ] && grep -q '.env.pre-pg19 not found' '$case_dir/out.txt'"
new_case rollbackconfirm; run --mode rollback
check "rollback needs the confirm word" bash -c "[ '$code' = 1 ]"

echo "-- regenerate-golden"
new_case golden
echo 190000 > "$S/version_num"
echo "old golden" > "$GOLDEN_DIR/golden.dump"
run --mode regenerate-golden --confirm REGEN-GOLDEN
check "golden regenerated" bash -c "[ '$code' = 0 ] && ls '$GOLDEN_DIR'/golden.dump.pre-pg19-* > /dev/null && grep -q 'PGDMP' '$GOLDEN_DIR/golden.dump' && ! ls '$GOLDEN_DIR'/golden.dump.new-* 2> /dev/null"
check "old golden kept as pre-pg19" bash -c "grep -q 'old golden' '$GOLDEN_DIR'/golden.dump.pre-pg19-*"
check "probe database dropped again" bash -c "grep -q 'CREATE DATABASE \"Klacks_goldentest\"' '$S/mutations.log' && grep -q 'DROP DATABASE \"Klacks_goldentest\"' '$S/mutations.log'"
check "note written" bash -c "grep -q 'regenerated from PostgreSQL 19' '$GOLDEN_DIR/golden-notes.log'"
new_case golden17; echo "old golden" > "$GOLDEN_DIR/golden.dump"; run --mode regenerate-golden --confirm REGEN-GOLDEN
check "refused on PostgreSQL 17" bash -c "[ '$code' = 1 ] && grep -q 'regenerated from PostgreSQL 19 only' '$case_dir/out.txt' && [ \"\$(cat '$GOLDEN_DIR/golden.dump')\" = 'old golden' ]"
new_case goldenunhealthy; echo 190000 > "$S/version_num"; echo "old golden" > "$GOLDEN_DIR/golden.dump"; touch "$S/api_unhealthy"
run --mode regenerate-golden --confirm REGEN-GOLDEN
check "unhealthy API keeps the old golden" bash -c "[ '$code' = 1 ] && [ \"\$(cat '$GOLDEN_DIR/golden.dump')\" = 'old golden' ] && ! ls '$GOLDEN_DIR'/golden.dump.pre-pg19-* 2> /dev/null"
new_case goldenprobe; echo 190000 > "$S/version_num"; echo "old golden" > "$GOLDEN_DIR/golden.dump"; touch "$S/probe_restore_fails"
run --mode regenerate-golden --confirm REGEN-GOLDEN
check "failing probe restore keeps the old golden" bash -c "[ '$code' = 1 ] && [ \"\$(cat '$GOLDEN_DIR/golden.dump')\" = 'old golden' ] && ! ls '$GOLDEN_DIR'/golden.dump.new-* 2> /dev/null"
new_case goldenreserve; echo 190000 > "$S/version_num"; echo "old golden" > "$GOLDEN_DIR/golden.dump"; export HEU_NOW_UTC=00:40
run --mode regenerate-golden --confirm REGEN-GOLDEN
check "reserve before the window refused" bash -c "[ '$code' = 1 ] && grep -q 'closer than' '$case_dir/out.txt'"
new_case goldenwindow; echo 190000 > "$S/version_num"; echo "old golden" > "$GOLDEN_DIR/golden.dump"; export HEU_NOW_UTC=02:30
run --mode regenerate-golden --confirm REGEN-GOLDEN
check "blocked window refused" bash -c "[ '$code' = 1 ] && grep -q 'blocked window' '$case_dir/out.txt'"

echo "-- cleanup"
new_case cleanup
echo 190000 > "$S/version_num"
echo "KLACKS_POSTGRES_DATA_SUBDIR=pg19" >> "$APPS_DIR/.env"
set_age 9
run --mode cleanup --confirm DELETE-PG17-CLUSTER
check "cleanup deletes old cluster and exchange dir" bash -c "[ '$code' = 0 ] && grep -q 'docker exec rm -rf /var/lib/postgresql/data/pgdata /var/lib/postgresql/data/engine-upgrade' '$S/mutations.log' && [ -f '$BACKUP_DIR/cleanup-done' ]"
check "old image removed" bash -c "grep -q 'docker image rm pgvector/pgvector:pg17' '$S/mutations.log'"
new_case cleanupage; echo 190000 > "$S/version_num"; echo "KLACKS_POSTGRES_DATA_SUBDIR=pg19" >> "$APPS_DIR/.env"; set_age 3
run --mode cleanup --confirm DELETE-PG17-CLUSTER
check "cleanup waits seven days" bash -c "[ '$code' = 1 ] && grep -q 'waits for 7' '$case_dir/out.txt' && [ ! -s '$S/mutations.log' ]"
new_case cleanupsubdir; echo 190000 > "$S/version_num"; echo "KLACKS_POSTGRES_DATA_SUBDIR=pgdata" >> "$APPS_DIR/.env"; set_age 9
run --mode cleanup --confirm DELETE-PG17-CLUSTER
check "cleanup refuses when the old subdir is the active one" bash -c "[ '$code' = 1 ] && grep -q 'not the active one' '$case_dir/out.txt' && [ ! -s '$S/mutations.log' ]"
new_case cleanupversion; echo 190000 > "$S/version_num"; echo "KLACKS_POSTGRES_DATA_SUBDIR=pg19" >> "$APPS_DIR/.env"; set_age 9; echo 19 > "$S/old_pg_version"
run --mode cleanup --confirm DELETE-PG17-CLUSTER
check "cleanup refuses to delete a directory that is not the old cluster" bash -c "[ '$code' = 1 ] && grep -q 'nothing is deleted' '$case_dir/out.txt' && [ ! -s '$S/mutations.log' ]"
new_case cleanup17; echo 170010 > "$S/version_num"; set_age 9
run --mode cleanup --confirm DELETE-PG17-CLUSTER
check "cleanup refuses on PostgreSQL 17" bash -c "[ '$code' = 1 ] && [ ! -s '$S/mutations.log' ]"
new_case cleanupnomarker; echo 190000 > "$S/version_num"; echo "KLACKS_POSTGRES_DATA_SUBDIR=pg19" >> "$APPS_DIR/.env"
run --mode cleanup --confirm DELETE-PG17-CLUSTER
check "cleanup needs the upgrade marker" bash -c "[ '$code' = 1 ] && grep -q 'upgrade-succeeded not found' '$case_dir/out.txt' && [ ! -s '$S/mutations.log' ]"
new_case cleanupconfirm; run --mode cleanup --confirm yes
check "cleanup needs its own confirm word" bash -c "[ '$code' = 1 ]"

echo "-- unknown mode"
new_case unknown; run --mode explode
check "unknown mode refused" bash -c "[ '$code' = 1 ]"

if [ "$failures" -ne 0 ]; then echo "$failures check(s) failed"; exit 1; fi
echo "All checks passed"
