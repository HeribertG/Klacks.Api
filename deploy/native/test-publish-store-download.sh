#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Tests publish-store-download.sh without a server: ssh and scp are replaced by local stubs, the "public URL" is a local
# python http.server (a redirecting path answers 302). Covers first placement, re-run with the same file, refusal to
# replace a different file, a corrupted upload, a missing public file, a redirecting public URL, and that no temporary
# upload is left behind.
#
# Usage: test-publish-store-download.sh   (needs bash, python3, curl, sha256sum)

set -euo pipefail

readonly SCRIPT="$(cd "$(dirname "$0")" && pwd)/publish-store-download.sh"
readonly PORT=18731
readonly VERSION='1.0.66'
work="$(mktemp -d)"
server_pid=""
failures=0

cleanup() {
  if [ -n "$server_pid" ]; then kill "$server_pid" 2>/dev/null || true; fi
  rm -rf "$work"
}
trap cleanup EXIT

mkdir -p "$work/bin" "$work/srv" "$work/web"
cat > "$work/bin/ssh" <<'STUB'
#!/usr/bin/env bash
while [ $# -gt 0 ]; do case "$1" in -i|-o|-p) shift 2 ;; *@*) shift; break ;; *) shift ;; esac; done
exec bash -c "$*"
STUB
cat > "$work/bin/scp" <<'STUB'
#!/usr/bin/env bash
args=()
while [ $# -gt 0 ]; do case "$1" in -i|-o|-P) shift 2 ;; -q) shift ;; *) args+=("$1"); shift ;; esac; done
cp "${args[0]}" "${args[1]#*:}"
if [ "${STUB_CORRUPT_UPLOAD:-0}" = "1" ]; then echo corrupt >> "${args[1]#*:}"; fi
STUB
chmod +x "$work/bin/ssh" "$work/bin/scp"
ln -s "$work/srv" "$work/web/dl"
cat > "$work/server.py" <<'PY'
import http.server, sys, os
class Handler(http.server.SimpleHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith('/redirect/'):
            self.send_response(302)
            self.send_header('Location', '/dl/' + self.path[len('/redirect/'):])
            self.end_headers()
            return
        super().do_GET()
    def log_message(self, *args):
        pass
os.chdir(sys.argv[2])
http.server.ThreadingHTTPServer(('127.0.0.1', int(sys.argv[1])), Handler).serve_forever()
PY
python3 "$work/server.py" "$PORT" "$work/web" &
server_pid=$!
sleep 1

export PATH="$work/bin:$PATH"
export STORE_SSH_HOST=host STORE_SSH_USER=user STORE_SSH_KEY_FILE="$work/key"
export STORE_DOWNLOAD_ROOT="$work/srv" STORE_PUBLIC_BASE_URL="http://127.0.0.1:${PORT}/dl"
export STORE_VERIFY_ATTEMPTS=1 STORE_VERIFY_RETRY_SECONDS=0
touch "$work/key"
exe="$work/Klacks-Setup-win-x64.exe"
placed="$work/srv/windows/${VERSION}/Klacks-Setup-win-x64.exe"

expect() {
  local name="$1" expected="$2"; shift 2
  local actual=0
  "$@" >"$work/out" 2>&1 || actual=$?
  if [ "$actual" -eq "$expected" ]; then
    echo "ok   $name"
  else
    echo "FAIL $name: exit $actual, expected $expected"; sed 's/^/     /' "$work/out"
    failures=$((failures + 1))
  fi
}

check() {
  local name="$1"; shift
  if "$@"; then echo "ok   $name"; else echo "FAIL $name"; failures=$((failures + 1)); fi
}

echo first > "$exe"
expect "places the file" 0 bash "$SCRIPT" "$exe" "$VERSION"
check "placed file is the uploaded one" cmp -s "$exe" "$placed"
check "placed file mode 0444" test "$(stat -c %a "$placed")" = 444

expect "same file again is accepted" 0 bash "$SCRIPT" "$exe" "$VERSION"

echo second > "$exe"
expect "different file is refused" 1 bash "$SCRIPT" "$exe" "$VERSION"
check "existing file is unchanged" grep -qx first "$placed"

STUB_CORRUPT_UPLOAD=1 expect "corrupted upload is refused" 1 bash "$SCRIPT" "$exe" 1.0.67
check "corrupted upload is not placed" test ! -e "$work/srv/windows/1.0.67"

STORE_PUBLIC_BASE_URL="http://127.0.0.1:${PORT}/missing" expect "missing public file fails" 1 bash "$SCRIPT" "$exe" 1.0.68
STORE_PUBLIC_BASE_URL="http://127.0.0.1:${PORT}/redirect" expect "redirecting public URL fails" 1 bash "$SCRIPT" "$exe" 1.0.69

expect "malformed version is a usage error" 2 bash "$SCRIPT" "$exe" latest
check "no temporary upload is left" test -z "$(find "$work/srv" -name '.incoming-*')"

if [ "$failures" -ne 0 ]; then
  echo "$failures test(s) failed"
  exit 1
fi
echo "all tests passed"
