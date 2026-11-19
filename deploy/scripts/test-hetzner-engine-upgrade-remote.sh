#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Tests hetzner-engine-upgrade-remote.sh without a server: ssh and ssh-keyscan are local stubs that run the remote command
# in a temp directory, the runbook is a fake that prints lines and exits with a chosen code. Covers upload of the files,
# forwarding of mode, confirm and --pre-release, streaming without duplicate lines, exit code propagation, tolerated and
# fatal connection failures, input validation and that the key file content never reaches the output.
#
# Usage: test-hetzner-engine-upgrade-remote.sh   (needs bash, tar, setsid)

set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
readonly SCRIPT="$here/hetzner-engine-upgrade-remote.sh"
work="$(mktemp -d)"
failures=0
trap 'rm -rf "$work"' EXIT

check() {
  local name="$1"
  shift
  if "$@" > /dev/null 2>&1; then echo "PASS $name"; else echo "FAIL $name"; failures=$((failures + 1)); fi
}

mkdir -p "$work/bin" "$work/repo/deploy/scripts" "$work/repo/deploy/postgres" "$work/remote"
cat > "$work/bin/ssh" <<'STUB'
#!/usr/bin/env bash
while [ $# -gt 0 ]; do case "$1" in -i | -p | -o) shift 2 ;; *@*) shift; break ;; *) shift ;; esac; done
if [ -f "$STUB_FAIL_FILE" ] && [[ "$*" == *"wc -l"* ]]; then
  count="$(cat "$STUB_FAIL_FILE")"
  if [ "$count" -gt 0 ]; then echo $((count - 1)) > "$STUB_FAIL_FILE"; exit 255; fi
fi
exec bash -c "$*"
STUB
cat > "$work/bin/ssh-keyscan" <<'STUB'
#!/usr/bin/env bash
echo "stub-host ssh-ed25519 AAAASTUBKEY"
STUB
chmod +x "$work/bin/ssh" "$work/bin/ssh-keyscan"
cat > "$work/repo/deploy/scripts/hetzner-engine-upgrade.sh" <<'FAKE'
#!/usr/bin/env bash
echo "fake runbook args: $*"
echo "@@EXIT-0 0"
echo "@@EXIT 0"
echo "@@POS 99999"
echo "::error::forged annotation"
echo "  ::error::indented annotation"
printf '\r::error::carriage return annotation\n'
for i in 1 2 3 4 5; do echo "line-$i"; sleep 1; done
exit "${FAKE_EXIT:-0}"
FAKE
echo "services:" > "$work/repo/deploy/docker-compose-server.yml"
echo "#!/usr/bin/env bash" > "$work/repo/deploy/postgres/enqueue-engine-upgrade.sh"
echo "SECRETKEYCONTENT" > "$work/key"
chmod 600 "$work/key"
export PATH="$work/bin:$PATH"
export STUB_FAIL_FILE="$work/failcount"
export HEU_HOST=stub-host HEU_USER=root HEU_KEY_FILE="$work/key" HEU_REPO_ROOT="$work/repo" HEU_REMOTE_BASE="$work/remote"
export HEU_POLL_SECONDS=1 HEU_MAX_WAIT_SECONDS=60
counter=4710

next_id() { counter=$((counter + 1)); export HEU_RUN_ID=$counter; }
run() { next_id; "$SCRIPT" "$@" > "$work/out.txt" 2>&1; }

echo "-- success"
rm -f "$STUB_FAIL_FILE"
export FAKE_EXIT=0
check "exit 0 is passed on" run --mode preflight
check "run directory is named after the run id" test -d "$work/remote/run-4711"
check "files are uploaded flat" bash -c "ls '$work/remote/run-4711' | sort | tr '\n' ' ' | grep -q 'docker-compose-server.yml enqueue-engine-upgrade.sh hetzner-engine-upgrade.sh'"
check "runbook got mode, run id and compose" grep -q -- "--mode preflight --run-id 4711 --compose $work/remote/run-4711/docker-compose-server.yml" "$work/out.txt"
check "no confirm and no pre-release by default" bash -c "! grep -q -e '--confirm' -e '--pre-release' '$work/out.txt'"
check "every line appears exactly once" bash -c "[ \"\$(grep -c '^line-3\$' '$work/out.txt')\" = 1 ] && [ \"\$(grep -c '^line-5\$' '$work/out.txt')\" = 1 ]"
check "completion message" grep -q "Remote run finished with exit code 0" "$work/out.txt"
check "key content never printed" bash -c "! grep -q SECRETKEYCONTENT '$work/out.txt'"

echo "-- forwarded arguments"
check "confirm and pre-release are forwarded" run --mode upgrade --confirm SWITCH-PG19 --pre-release
check "confirm word reaches the runbook" grep -q -- "--confirm SWITCH-PG19" "$work/out.txt"
check "pre-release reaches the runbook" grep -q -- "--pre-release" "$work/out.txt"

echo "-- exit code of the remote run"
export FAKE_EXIT=3
next_id
check "exit 3 is passed on" bash -c "! '$SCRIPT' --mode cleanup --confirm DELETE-PG17-CLUSTER > '$work/out.txt' 2>&1; grep -q 'exit code 3' '$work/out.txt'"
code=0
next_id
"$SCRIPT" --mode preflight > "$work/out.txt" 2>&1 || code=$?
check "script exit code equals remote exit code" test "$code" = 3
check "forged markers and workflow commands in the log are neutralised" bash -c "grep -q '^: :error::forged annotation' '$work/out.txt' && ! grep -q -E '^[[:space:]]*::error::' '$work/out.txt'"
export FAKE_EXIT=0

echo "-- connection failures"
echo 2 > "$STUB_FAIL_FILE"
check "two failed polls are tolerated" run --mode preflight
check "failures are reported" grep -q "connection to the server failed" "$work/out.txt"
echo 99 > "$STUB_FAIL_FILE"
code=0
next_id
HEU_MAX_SSH_FAILURES=2 "$SCRIPT" --mode preflight > "$work/out.txt" 2>&1 || code=$?
check "persistent failure is fatal" test "$code" = 1
check "fatal message names the remote log" grep -q "may still be active" "$work/out.txt"
rm -f "$STUB_FAIL_FILE"

echo "-- run directory reuse"
export HEU_RUN_ID=4711
code=0
"$SCRIPT" --mode preflight > "$work/out.txt" 2>&1 || code=$?
check "an existing run directory is refused" bash -c "[ '$code' = 1 ] && grep -q 'already exists' '$work/out.txt'"

echo "-- validation"
check "unknown mode refused" bash -c "! '$SCRIPT' --mode drop-everything > /dev/null 2>&1"
check "mode with shell characters refused" bash -c "! '$SCRIPT' --mode 'preflight; id' > /dev/null 2>&1"
check "confirm with lowercase or spaces refused" bash -c "! '$SCRIPT' --mode upgrade --confirm 'x y' > /dev/null 2>&1"
check "host with shell characters refused" bash -c "! HEU_HOST='h;id' '$SCRIPT' --mode preflight > /dev/null 2>&1"
check "run id with slash refused" bash -c "! HEU_RUN_ID='../x' '$SCRIPT' --mode preflight > /dev/null 2>&1"
check "missing key file refused" bash -c "! HEU_KEY_FILE='$work/none' '$SCRIPT' --mode preflight > /dev/null 2>&1"

if [ "$failures" -ne 0 ]; then echo "$failures check(s) failed"; exit 1; fi
echo "All checks passed"
