#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Inserts the engine block written by New-EngineManifestBlock.sh into a stable.json as artifacts.postgres. Fail-closed:
# the block must look complete (numeric major, Docker image, sha256 digest, signature), the manifest must already carry
# artifacts, and every field outside artifacts.postgres must be identical before and after. On any error stable.json is
# left untouched and the exit code is not zero.
#
# Usage: Merge-EngineManifestBlock.sh --manifest <stable.json> --block <postgres-block.json>
# Needs bash and jq.

set -euo pipefail

manifest=""
block=""
while [ $# -gt 0 ]; do
  case "$1" in
    --manifest) manifest="${2:?}"; shift 2 ;;
    --block) block="${2:?}"; shift 2 ;;
    *) echo "Unknown argument '$1'" >&2; exit 2 ;;
  esac
done
if [ ! -f "$manifest" ] || [ ! -f "$block" ]; then
  echo "Usage: $0 --manifest <stable.json> --block <postgres-block.json>" >&2
  exit 2
fi

jq -e '(.major | type) == "number" and (.docker.image | type) == "string" and (.docker.sha256 | test("^sha256:[0-9a-f]{64}$"))
  and ((.docker.signature // "") | length) > 0 and (.native | type) == "array"' "$block" > /dev/null \
  || { echo "Engine block is incomplete, manifest not changed." >&2; exit 1; }
jq -e '(.artifacts | type) == "object"' "$manifest" > /dev/null \
  || { echo "Manifest has no artifacts object, not changed." >&2; exit 1; }

merged="$(mktemp)"
trap 'rm -f "$merged"' EXIT
jq --slurpfile block "$block" '.artifacts.postgres = $block[0]' "$manifest" > "$merged"

before="$(jq -S 'del(.artifacts.postgres)' "$manifest")"
after="$(jq -S 'del(.artifacts.postgres)' "$merged")"
[ "$before" = "$after" ] || { echo "Merge changed fields outside artifacts.postgres, manifest not changed." >&2; exit 1; }

cp "$merged" "$manifest"
echo "Merged artifacts.postgres (major $(jq '.artifacts.postgres.major' "$manifest")) into $manifest"
