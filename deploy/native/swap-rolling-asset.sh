#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Replaces one asset of a GitHub release without a window in which its public download URL is missing: the new file is
# uploaded under a staging name, the old asset is renamed aside and the staged one takes the public name. A failed swap
# restores the old asset; a leftover ".previous" asset from a dead run is restored when no public asset exists, else deleted.
# The previous asset is kept until the next run because GitHub caches the redirect of the public URL for a few minutes.
# Same procedure as the inline package swap in .github/workflows/native-windows.yml, for assets that are not zip files.
#
# Usage: swap-rolling-asset.sh <repo> <release-tag> <local-file> <public-name> <staged-name> <previous-name>
# Environment: GH_TOKEN (gh CLI).

set -euo pipefail

readonly RENAME_ATTEMPTS=3
readonly RENAME_RETRY_SECONDS=10

if [ "$#" -ne 6 ]; then
  echo "Usage: $0 <repo> <release-tag> <local-file> <public-name> <staged-name> <previous-name>" >&2
  exit 2
fi

repo="$1"
tag="$2"
file="$3"
public_name="$4"
staged_name="$5"
previous_name="$6"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

asset_url() {
  jq -r --arg n "$1" '.assets[] | select(.name == $n) | .apiUrl' "$work/assets.json"
}

rename_asset() {
  local attempt
  for attempt in $(seq 1 "$RENAME_ATTEMPTS"); do
    if gh api -X PATCH "$1" -f name="$2" > /dev/null; then return 0; fi
    sleep "$RENAME_RETRY_SECONDS"
  done
  return 1
}

cp "$file" "$work/$staged_name"
gh release upload "$tag" "$work/$staged_name" -R "$repo" --clobber

gh release view "$tag" -R "$repo" --json assets > "$work/assets.json"
old="$(asset_url "$public_name")"
leftover="$(asset_url "$previous_name")"

if [ -n "$leftover" ] && [ -z "$old" ]; then
  rename_asset "$leftover" "$public_name" || { echo "::error::Restoring ${previous_name} on ${tag} failed"; exit 1; }
  old="$leftover"
elif [ -n "$leftover" ]; then
  gh api -X DELETE "$leftover"
fi

staged="$(asset_url "$staged_name")"
[ -n "$staged" ] || { echo "::error::Staged asset ${staged_name} is missing on ${tag}"; exit 1; }

if [ -n "$old" ]; then
  rename_asset "$old" "$previous_name" || { echo "::error::Moving the previous asset aside on ${tag} failed"; exit 1; }
fi

if ! rename_asset "$staged" "$public_name"; then
  if [ -n "$old" ]; then rename_asset "$old" "$public_name" || echo "::error::Restoring the previous asset on ${tag} failed"; fi
  echo "::error::Renaming the staged asset on ${tag} failed"
  exit 1
fi

gh release download "$tag" -R "$repo" -p "$public_name" -O "$work/back" --clobber
cmp "$work/back" "$file"
echo "Release ${tag} serves ${public_name}."
