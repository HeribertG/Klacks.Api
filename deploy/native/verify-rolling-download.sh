#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Read-only check that the rolling prerelease (office-pc) serves the given release: its title names that version, and
# every listed asset exists on the rolling release and on the version release v<version> with the same SHA-256 digest.
# The version release is the reference because the publish job byte-compares its ZIP and EXE with the files built in
# the run. A rolling title that names a NEWER version is not a failure (a later release has already replaced it).
# An asset given with --may-lag (the fail-soft win-arm64 files) may also serve the previous stable release v<previous>:
# its rolling digest must equal the one on v<version> or on v<previous>; when neither of the two releases has it, it has
# not been published yet and only a warning is written. Lagging two or more versions is stale.
# Nothing is changed on either release: a stale download stays on the last good version.
#
# Usage: verify-rolling-download.sh <repo> <version> <rolling-tag> <asset-name>... [--may-lag <asset-name>]...
# Environment: GH_TOKEN (gh CLI); GITHUB_STEP_SUMMARY (optional, gets a short report);
#              VERIFY_DIGEST_RETRY_SECONDS (optional, wait between reads while a digest is missing; tests set 0).
# Exit codes: 0 = serves this version or a newer one, 1 = stale or incomplete, 2 = usage error.

set -euo pipefail

readonly EXIT_STALE=1
readonly EXIT_USAGE=2
readonly MIN_ARGS=4
readonly MAY_LAG_FLAG='--may-lag'
readonly STABLE_TAGS_FILTER='.[] | select((.draft or .prerelease) | not) | .tag_name'
readonly VERSION_PATTERN='[0-9]+\.[0-9]+\.[0-9]+'
readonly RELEASE_LISTING_FILTER='"title\t\(.name)", (.assets[] | "asset\t\(.name)\t\(.digest // "")")'
readonly DIGEST_READ_ATTEMPTS=6
readonly DEFAULT_DIGEST_RETRY_SECONDS=20
readonly DIGEST_RETRY_SECONDS="${VERIFY_DIGEST_RETRY_SECONDS:-$DEFAULT_DIGEST_RETRY_SECONDS}"

if [ "$#" -lt "$MIN_ARGS" ]; then
  echo "Usage: $0 <repo> <version> <rolling-tag> <asset-name>... [${MAY_LAG_FLAG} <asset-name>]..." >&2
  exit "$EXIT_USAGE"
fi

repo="$1"
version="${2#v}"
rolling="$3"
shift 3
assets=()
lag_assets=()
while [ "$#" -gt 0 ]; do
  if [ "$1" = "$MAY_LAG_FLAG" ]; then
    if [ "$#" -lt 2 ]; then
      echo "${MAY_LAG_FLAG} needs an asset name" >&2
      exit "$EXIT_USAGE"
    fi
    lag_assets+=("$2")
    shift 2
  else
    assets+=("$1")
    shift
  fi
done
if [ "${#assets[@]}" -eq 0 ]; then
  echo "At least one asset must be checked strictly (without ${MAY_LAG_FLAG})." >&2
  exit "$EXIT_USAGE"
fi
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
  for name in "${assets[@]}" "${lag_assets[@]}"; do
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

# Highest stable release below $version; empty when there is none. STABLE_TAGS_FILTER drops drafts (dry runs) and
# prereleases (the rolling office-pc itself, -rc tags), the tag pattern keeps only vX.Y.Z. awk reads all of its input,
# so no stage of the pipe can die of SIGPIPE.
previous_version() {
  gh api "repos/${repo}/releases" --paginate --jq "$STABLE_TAGS_FILTER" > "$work/tags.txt" 2> "$work/gh-error.txt" || return 1
  { grep -Ex "v${VERSION_PATTERN}" "$work/tags.txt" | sed 's/^v//' || true; echo "$version"; }     | sort -uV | awk -v v="$version" '$0 == v { found = prev } { prev = $0 } END { if (found != "") print found }'
}

lag_notes=()
previous=""
previous_read=""
for name in "${lag_assets[@]}"; do
  rolling_digest="$(asset_digest "$work/rolling.tsv" "$name" || true)"
  version_digest="$(asset_digest "$work/version.tsv" "$name" || true)"
  if [ -n "$rolling_digest" ] && [ "$rolling_digest" = "$version_digest" ]; then
    echo "${name}: ${rolling} = ${version_tag} (${rolling_digest})"
    continue
  fi
  if [ -z "$previous_read" ]; then
    previous_read=yes
    if ! previous="$(previous_version)"; then
      findings+=("Listing the releases of ${repo} failed, so ${name} cannot be checked against the previous release: $(tr '\n' ' ' < "$work/gh-error.txt")")
      break
    fi
    if [ -n "$previous" ] && ! read_release "v${previous}" "$work/previous.tsv"; then
      findings+=("Reading release v${previous} of ${repo} failed: $(tr '\n' ' ' < "$work/gh-error.txt")")
      break
    fi
  fi
  previous_digest=""
  if [ -n "$previous" ]; then previous_digest="$(asset_digest "$work/previous.tsv" "$name" || true)"; fi
  previous_label="${previous:+v${previous}}"
  previous_label="${previous_label:-a previous release}"
  if [ -n "$rolling_digest" ] && [ -n "$previous_digest" ] && [ "$rolling_digest" = "$previous_digest" ]; then
    echo "::notice::${name} on ${rolling} is the build of ${previous_label}, one version behind ${version} (allowed for this asset)."
    lag_notes+=("${name} lags one version (serves ${previous_label}).")
    continue
  fi
  if ! asset_digest "$work/rolling.tsv" "$name" > /dev/null && [ -z "$version_digest" ] && [ -z "$previous_digest" ]; then
    echo "::warning::${name} is on neither ${version_tag} nor ${previous_label}, nor on ${rolling}: not published yet."
    lag_notes+=("${name} is not published yet (missing on ${version_tag} and ${previous_label}).")
    continue
  fi
  if [ -z "$rolling_digest" ]; then
    findings+=("${name} is missing on ${rolling} or has no SHA-256 digest, although ${version_tag} or ${previous_label} publishes it.")
  else
    findings+=("${name} on ${rolling} (${rolling_digest}) is neither the ${version} build nor the build of ${previous_label}: it lags more than one version.")
  fi
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

{
  echo "### Office PC download check"; echo; echo "${rolling} serves ${version}: ${assets[*]} match ${version_tag}."
  for note in "${lag_notes[@]}"; do echo "- ${note}"; done
} >> "$summary"
echo "${rolling} serves ${version}: ${assets[*]} match ${version_tag}."
for note in "${lag_notes[@]}"; do echo "${note}"; done
