#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Read-only check that the rolling prerelease (office-pc) serves the given release: its title names that version, and
# every listed asset exists on the rolling release and on the version release v<version> with the same SHA-256 digest.
# The version release is the reference because the publish job byte-compares its ZIP and EXE with the files built in
# the run. A rolling title that names a NEWER version is not a failure (a later release has already replaced it).
# Nothing is changed on either release: a stale download stays on the last good version.
#
# Usage: verify-rolling-download.sh <repo> <version> <rolling-tag> <asset-name>...
# Environment: GH_TOKEN (gh CLI); GITHUB_STEP_SUMMARY (optional, gets a short report);
#              VERIFY_DIGEST_RETRY_SECONDS (optional, wait between reads while a digest is missing; tests set 0).
# Exit codes: 0 = serves this version or a newer one, 1 = stale or incomplete, 2 = usage error.

set -euo pipefail

readonly EXIT_STALE=1
readonly EXIT_USAGE=2
readonly MIN_ARGS=4
readonly VERSION_PATTERN='[0-9]+\.[0-9]+\.[0-9]+'
readonly RELEASE_LISTING_FILTER='"title\t\(.name)", (.assets[] | "asset\t\(.name)\t\(.digest // "")")'
readonly DIGEST_READ_ATTEMPTS=6
readonly DEFAULT_DIGEST_RETRY_SECONDS=20
readonly DIGEST_RETRY_SECONDS="${VERIFY_DIGEST_RETRY_SECONDS:-$DEFAULT_DIGEST_RETRY_SECONDS}"

if [ "$#" -lt "$MIN_ARGS" ]; then
  echo "Usage: $0 <repo> <version> <rolling-tag> <asset-name>..." >&2
  exit "$EXIT_USAGE"
fi

repo="$1"
version="${2#v}"
rolling="$3"
shift 3
assets=("$@")
version_tag="v${version}"
summary="${GITHUB_STEP_SUMMARY:-/dev/null}"

if ! printf '%s' "$version" | grep -Eqx "$VERSION_PATTERN"; then
  echo "Invalid version '${version}', expected X.Y.Z" >&2
  exit "$EXIT_USAGE"
fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

fail_now() {
  echo "::error::$1"
  { echo "### Office PC download check: FAILED"; echo; echo "- $1"; } >> "$summary"
  exit "$EXIT_STALE"
}

read_release() {
  gh api "repos/${repo}/releases/tags/$1" --jq "$RELEASE_LISTING_FILTER" > "$2" 2> "$work/gh-error.txt"
}

asset_digest() {
  awk -F '\t' -v n="$2" '$1 == "asset" && $2 == n { found = 1; print $3 } END { if (!found) exit 1 }' "$1"
}

read_release "$rolling" "$work/rolling.tsv" \
  || fail_now "Reading release ${rolling} of ${repo} failed: $(tr '\n' ' ' < "$work/gh-error.txt")"

title="$(awk -F '\t' '$1 == "title" { print $2; exit }' "$work/rolling.tsv")"
served="$(printf '%s' "$title" | grep -Eo "${VERSION_PATTERN}\$" || true)"
[ -n "$served" ] || fail_now "The title '${title}' of ${rolling} names no version; the download state is unknown."

if [ "$served" != "$version" ]; then
  if [ "$(printf '%s\n%s\n' "$served" "$version" | sort -V | tail -1)" = "$served" ]; then
    echo "::notice::${rolling} already serves ${served}, newer than ${version}; nothing to check."
    { echo "### Office PC download check"; echo; echo "${rolling} serves ${served}, newer than ${version}."; } >> "$summary"
    exit 0
  fi
  fail_now "${rolling} still serves ${served}, expected ${version}: the public office-PC download was not replaced."
fi

read_release "$version_tag" "$work/version.tsv" \
  || fail_now "Reading release ${version_tag} of ${repo} failed: $(tr '\n' ' ' < "$work/gh-error.txt")"

digest_pending() {
  local name listing
  for name in "${assets[@]}"; do
    for listing in "$work/rolling.tsv" "$work/version.tsv"; do
      if awk -F '\t' -v n="$name" '$1 == "asset" && $2 == n && $3 == "" { found = 1 } END { exit !found }' "$listing"; then
        return 0
      fi
    done
  done
  return 1
}

# GitHub may fill in the SHA-256 digest of a fresh upload a moment later: re-read both listings a bounded number of
# times before an asset without a digest counts as unverifiable.
attempt=1
while digest_pending && [ "$attempt" -lt "$DIGEST_READ_ATTEMPTS" ]; do
  echo "An asset has no SHA-256 digest yet; reading again in ${DIGEST_RETRY_SECONDS}s (attempt ${attempt}/${DIGEST_READ_ATTEMPTS})."
  sleep "$DIGEST_RETRY_SECONDS"
  attempt=$((attempt + 1))
  read_release "$rolling" "$work/rolling.tsv" \
    || fail_now "Reading release ${rolling} of ${repo} failed: $(tr '\n' ' ' < "$work/gh-error.txt")"
  read_release "$version_tag" "$work/version.tsv" \
    || fail_now "Reading release ${version_tag} of ${repo} failed: $(tr '\n' ' ' < "$work/gh-error.txt")"
done

findings=()
for name in "${assets[@]}"; do
  if ! rolling_digest="$(asset_digest "$work/rolling.tsv" "$name")"; then
    findings+=("${name} is missing on ${rolling}.")
    continue
  fi
  if ! version_digest="$(asset_digest "$work/version.tsv" "$name")"; then
    findings+=("${name} is missing on ${version_tag}, so ${rolling} cannot serve the ${version} build of it.")
    continue
  fi
  if [ -z "$rolling_digest" ] || [ -z "$version_digest" ]; then
    findings+=("${name} has no SHA-256 digest on ${rolling} or ${version_tag} after ${DIGEST_READ_ATTEMPTS} reads; it cannot be verified.")
    continue
  fi
  if [ "$rolling_digest" != "$version_digest" ]; then
    findings+=("${name} on ${rolling} (${rolling_digest}) differs from ${version_tag} (${version_digest}): it is not the ${version} build.")
    continue
  fi
  echo "${name}: ${rolling} = ${version_tag} (${rolling_digest})"
done

if [ "${#findings[@]}" -gt 0 ]; then
  { echo "### Office PC download check: FAILED"; echo; echo "${rolling} is titled ${version} but does not serve all of its files."; echo; } >> "$summary"
  for finding in "${findings[@]}"; do
    echo "::error::${finding}"
    echo "- ${finding}" >> "$summary"
  done
  { echo; echo "The download was left unchanged. Repair: re-run all jobs of the native release run, or swap the asset by hand."; } >> "$summary"
  exit "$EXIT_STALE"
fi

{ echo "### Office PC download check"; echo; echo "${rolling} serves ${version}: ${assets[*]} match ${version_tag}."; } >> "$summary"
echo "${rolling} serves ${version}: ${assets[*]} match ${version_tag}."
