#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Tests New-EngineManifestBlock.sh and Merge-EngineManifestBlock.sh without network or Docker: the "release" is a local
# python http.server, the Docker digest is passed in. Covers a valid Docker + native block (signatures verified
# independently with openssl against the byte-exact payload), a missing native entry, a bad signature, a SHA-256 mismatch,
# a wrong bundle URL, a native entry signed for another major, an invalid digest (no output file), an invalid engine.json,
# and the merge (fields outside artifacts.postgres unchanged, incomplete block and missing artifacts refused, manifest
# untouched on failure).
#
# Usage: test-engine-manifest-block.sh   (needs bash, jq, openssl, curl, sha256sum, python3)

set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
readonly BLOCK_SCRIPT="$here/New-EngineManifestBlock.sh"
readonly MERGE_SCRIPT="$here/Merge-EngineManifestBlock.sh"
readonly PORT=18742
readonly BASE="http://127.0.0.1:$PORT"
readonly TAG='postgres-engine-19.0-pgvector0.8.7'
readonly ZIP='klacks-postgres-19.0-win-x64.zip'
readonly ENTRY='klacks-postgres-19.0-win-x64.json'
readonly DIGEST='sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef'
work="$(mktemp -d)"
server_pid=""
failures=0

cleanup() {
  if [ -n "$server_pid" ]; then kill "$server_pid" 2>/dev/null || true; fi
  rm -rf "$work"
}
trap cleanup EXIT

check() {
  local name="$1"; shift
  if "$@" > /dev/null 2>&1; then echo "PASS $name"; else echo "FAIL $name"; failures=$((failures + 1)); fi
}

openssl genrsa -out "$work/key.pem" 2048 2> /dev/null
openssl pkey -in "$work/key.pem" -pubout -out "$work/pub.pem"
openssl genrsa -out "$work/other.pem" 2048 2> /dev/null

cat > "$work/engine.json" <<'JSON'
{ "major": 19, "version": "19.0", "pgvector": "0.8.7", "image": "ghcr.io/heribertg/klacks-postgres:19",
  "windowsBinariesUrl": "https://example.invalid/pg.zip", "releaseSuffix": "" }
JSON

independent_sign() { printf 'klacks-postgres-engine\n%s\n%s\n%s' "$2" "$3" "$4" | openssl dgst -sha256 -sign "$1" | base64 -w0; }
independent_verify() { # pubkey major ref sha sig
  printf '%s' "$5" | base64 -d > "$work/v.sig"
  printf 'klacks-postgres-engine\n%s\n%s\n%s' "$2" "$3" "$4" > "$work/v.payload"
  openssl dgst -sha256 -verify "$1" -signature "$work/v.sig" "$work/v.payload"
}

application_payload_rejected() {
  jq -r '.docker.signature' "$work/block.json" | base64 -d > "$work/app.sig"
  printf '%s
%s
%s' '19' 'ghcr.io/heribertg/klacks-postgres:19' "$DIGEST" > "$work/app.payload"
  ! openssl dgst -sha256 -verify "$work/pub.pem" -signature "$work/app.sig" "$work/app.payload" > /dev/null 2>&1
}

mkdir -p "$work/srv/$TAG"
head -c 4096 /dev/urandom > "$work/srv/$TAG/$ZIP"
zip_sha="$(sha256sum "$work/srv/$TAG/$ZIP" | cut -d' ' -f1)"
good_url="$BASE/$TAG/$ZIP"

write_entry() { # signature_key major sha url
  local sig
  sig="$(independent_sign "$1" "$2" "$4" "$3")"
  jq -n --arg url "$4" --arg sha "$3" --arg sig "$sig" '{rid: "win-x64", bundleUrl: $url, sha256: $sha, signature: $sig}' > "$work/srv/$TAG/$ENTRY"
}

python3 - "$PORT" "$work/srv" <<'PY' &
import http.server, sys, os
class Handler(http.server.SimpleHTTPRequestHandler):
    def log_message(self, *args):
        pass
os.chdir(sys.argv[2])
http.server.ThreadingHTTPServer(('127.0.0.1', int(sys.argv[1])), Handler).serve_forever()
PY
server_pid=$!
sleep 1

run_block() { "$BLOCK_SCRIPT" --engine-json "$work/engine.json" --private-key "$work/key.pem" --docker-digest "$DIGEST" --release-base "$BASE" --out "$work/block.json"; }

echo "-- valid docker + native"
write_entry "$work/key.pem" 19 "$zip_sha" "$good_url"
rm -f "$work/block.json"
check "script succeeds" run_block
check "block fields" jq -e '.major == 19 and .version == "19.0" and .pgvector == "0.8.7" and .docker.image == "ghcr.io/heribertg/klacks-postgres:19" and .docker.sha256 == "'"$DIGEST"'"' "$work/block.json"
check "docker signature verifies independently" independent_verify "$work/pub.pem" 19 "ghcr.io/heribertg/klacks-postgres:19" "$DIGEST" "$(jq -r '.docker.signature' "$work/block.json")"
check "native entry adopted" jq -e '.native | length == 1 and .[0].rid == "win-x64" and .[0].sha256 == "'"$zip_sha"'" and .[0].bundleUrl == "'"$good_url"'"' "$work/block.json"
check "native entry keeps only four keys" jq -e '.native[0] | keys == ["bundleUrl","rid","sha256","signature"]' "$work/block.json"
check "native signature verifies independently" independent_verify "$work/pub.pem" 19 "$good_url" "$zip_sha" "$(jq -r '.native[0].signature' "$work/block.json")"
check "payload is domain separated (application payload does not verify)" application_payload_rejected
check "no temporary file left" bash -c "[ ! -e '$work/block.json.tmp' ]"

echo "-- native entry absent"
mv "$work/srv/$TAG/$ENTRY" "$work/entry.bak"
rm -f "$work/block.json"
check "script still succeeds" run_block
check "docker entry present, native empty" jq -e '.docker.signature != "" and .native == []' "$work/block.json"
mv "$work/entry.bak" "$work/srv/$TAG/$ENTRY"

echo "-- native entry signed with another key"
write_entry "$work/other.pem" 19 "$zip_sha" "$good_url"
rm -f "$work/block.json"
check "bad signature: script succeeds" run_block
check "bad signature: native discarded" jq -e '.native == [] and (.docker.signature | length) > 0' "$work/block.json"

echo "-- native entry signed for another major"
write_entry "$work/key.pem" 20 "$zip_sha" "$good_url"
rm -f "$work/block.json"
check "wrong major: native discarded" bash -c "'$BLOCK_SCRIPT' --engine-json '$work/engine.json' --private-key '$work/key.pem' --docker-digest '$DIGEST' --release-base '$BASE' --out '$work/block.json' && jq -e '.native == []' '$work/block.json'"

echo "-- sha256 in entry does not match the ZIP"
wrong_sha="$(printf 'x' | sha256sum | cut -d' ' -f1)"
write_entry "$work/key.pem" 19 "$wrong_sha" "$good_url"
rm -f "$work/block.json"
check "sha mismatch: native discarded" bash -c "'$BLOCK_SCRIPT' --engine-json '$work/engine.json' --private-key '$work/key.pem' --docker-digest '$DIGEST' --release-base '$BASE' --out '$work/block.json' && jq -e '.native == []' '$work/block.json'"

echo "-- entry names another URL"
write_entry "$work/key.pem" 19 "$zip_sha" "$BASE/latest/download/$ZIP"
rm -f "$work/block.json"
check "wrong URL: native discarded" bash -c "'$BLOCK_SCRIPT' --engine-json '$work/engine.json' --private-key '$work/key.pem' --docker-digest '$DIGEST' --release-base '$BASE' --out '$work/block.json' && jq -e '.native == []' '$work/block.json'"

echo "-- skip-native"
write_entry "$work/key.pem" 19 "$zip_sha" "$good_url"
rm -f "$work/block.json"
check "skip-native: empty native list" bash -c "'$BLOCK_SCRIPT' --engine-json '$work/engine.json' --private-key '$work/key.pem' --docker-digest '$DIGEST' --release-base '$BASE' --skip-native --out '$work/block.json' && jq -e '.native == []' '$work/block.json'"

echo "-- invalid inputs write no file"
rm -f "$work/block.json"
check "invalid digest fails" bash -c "! '$BLOCK_SCRIPT' --engine-json '$work/engine.json' --private-key '$work/key.pem' --docker-digest 'sha256:ABC' --release-base '$BASE' --out '$work/block.json'"
check "invalid digest: no output file" bash -c "[ ! -e '$work/block.json' ]"
jq '.major = 18' "$work/engine.json" > "$work/engine-bad.json"
check "image tag not matching major fails" bash -c "! '$BLOCK_SCRIPT' --engine-json '$work/engine-bad.json' --private-key '$work/key.pem' --docker-digest '$DIGEST' --release-base '$BASE' --out '$work/block.json'"
jq '.releaseSuffix = "-latest"' "$work/engine.json" > "$work/engine-bad.json"
check "invalid releaseSuffix fails" bash -c "! '$BLOCK_SCRIPT' --engine-json '$work/engine-bad.json' --private-key '$work/key.pem' --docker-digest '$DIGEST' --release-base '$BASE' --out '$work/block.json'"
check "garbage private key fails" bash -c "echo nope > '$work/garbage.pem'; ! '$BLOCK_SCRIPT' --engine-json '$work/engine.json' --private-key '$work/garbage.pem' --docker-digest '$DIGEST' --skip-native --out '$work/block.json'"
check "still no output file" bash -c "[ ! -e '$work/block.json' ]"

echo "-- merge"
cat > "$work/stable.json" <<'JSON'
{
  "channel": "Stable",
  "latestVersion": "1.0.79",
  "containsMigrations": true,
  "artifacts": {
    "docker": { "apiImage": "ghcr.io/heribertg/klacks-api:1.0.79", "sha256": "sha256:aa" },
    "native": [ { "rid": "win-x64", "bundleUrl": "https://example.invalid/a.zip", "sha256": "bb", "signature": "cc+/=" } ]
  },
  "signature": "abc+/=="
}
JSON
write_entry "$work/key.pem" 19 "$zip_sha" "$good_url"
run_block > /dev/null 2>&1
cp "$work/stable.json" "$work/stable.orig.json"
check "merge succeeds" "$MERGE_SCRIPT" --manifest "$work/stable.json" --block "$work/block.json"
check "merge adds artifacts.postgres" jq -e --slurpfile b "$work/block.json" ".artifacts.postgres == \$b[0]" "$work/stable.json"
check "merge leaves everything else unchanged" bash -c "[ \"\$(jq -S 'del(.artifacts.postgres)' '$work/stable.json')\" = \"\$(jq -S . '$work/stable.orig.json')\" ]"
check "merge is idempotent" bash -c "'$MERGE_SCRIPT' --manifest '$work/stable.json' --block '$work/block.json' && jq -e '.artifacts.postgres.major == 19' '$work/stable.json'"
check "top-level signature byte identical" bash -c "[ \"\$(jq -r .signature '$work/stable.json')\" = 'abc+/==' ]"

cp "$work/stable.orig.json" "$work/stable.json"
jq 'del(.docker.signature)' "$work/block.json" > "$work/block-incomplete.json"
check "incomplete block refused" bash -c "! '$MERGE_SCRIPT' --manifest '$work/stable.json' --block '$work/block-incomplete.json'"
check "manifest untouched after refusal" bash -c "cmp -s '$work/stable.json' '$work/stable.orig.json'"
echo '{"channel":"Stable"}' > "$work/no-artifacts.json"
check "manifest without artifacts refused" bash -c "! '$MERGE_SCRIPT' --manifest '$work/no-artifacts.json' --block '$work/block.json'"
echo 'not json' > "$work/broken-block.json"
check "broken block refused" bash -c "! '$MERGE_SCRIPT' --manifest '$work/stable.json' --block '$work/broken-block.json'"
check "manifest untouched after broken block" bash -c "cmp -s '$work/stable.json' '$work/stable.orig.json'"

if [ "$failures" -ne 0 ]; then echo "$failures check(s) failed"; exit 1; fi
echo "All checks passed"
