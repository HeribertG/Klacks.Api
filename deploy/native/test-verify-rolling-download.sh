#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Local checks for verify-rolling-download.sh against a stub gh that serves canned release listings (no network, no
# GitHub writes): the green case, a newer rolling release, and every stale case - wrong or missing title version, EXE or
# ZIP from another build, an asset missing on either release, a digest that is missing for good (bounded re-reads,
# VERIFY_DIGEST_RETRY_SECONDS=0) or arrives on the second read. Run: bash deploy/native/test-verify-rolling-download.sh

set -euo pipefail

readonly SCRIPT="$(cd "$(dirname "$0")" && pwd)/verify-rolling-download.sh"
readonly REPO='HeribertG/Klacks.Api'
readonly ROLLING='office-pc'
readonly VERSION='1.0.49'
readonly ZIP='klacks-office-pc-win-x64.zip'
readonly EXE='Klacks-Setup-win-x64.exe'
readonly ZIP_SHA='sha256:aaaa'
readonly EXE_SHA='sha256:bbbb'
readonly OLD_SHA='sha256:0000'
readonly EXIT_OK=0
readonly EXIT_STALE=1
readonly FIRST_READ=1
readonly SECOND_READ=2
readonly DIGEST_READ_ATTEMPTS=6

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/bin"
cat > "$work/bin/gh" <<'STUB'
#!/usr/bin/env bash
tag="${2##*/}"
[ "$1" = api ] && [ -f "$FIXTURES/$tag.tsv" ] || { echo "gh: Not Found (HTTP 404)" >&2; exit 1; }
reads=$(( $(cat "$FIXTURES/$tag.reads" 2>/dev/null || echo 0) + 1 ))
echo "$reads" > "$FIXTURES/$tag.reads"
if [ -f "$FIXTURES/$tag.from-read-$reads.tsv" ]; then cp "$FIXTURES/$tag.from-read-$reads.tsv" "$FIXTURES/$tag.tsv"; fi
cat "$FIXTURES/$tag.tsv"
STUB
chmod +x "$work/bin/gh"

failures=0

listing() {
  printf 'title\t%s\n' "$1"
  shift
  while [ "$#" -gt 0 ]; do printf 'asset\t%s\t%s\n' "$1" "$2"; shift 2; done
}

run_case() {
  local name="$1" expected="$2" rolling_listing="$3" version_listing="$4" expected_text="$5"
  local later_rolling_listing="${6:-}" later_read="${7:-}" expected_rolling_reads="${8:-}"
  local fixtures="$work/$name"
  mkdir -p "$fixtures"
  [ -z "$later_rolling_listing" ] || printf '%s' "$later_rolling_listing" > "$fixtures/${ROLLING}.from-read-${later_read}.tsv"
  [ -z "$rolling_listing" ] || printf '%s' "$rolling_listing" > "$fixtures/${ROLLING}.tsv"
  [ -z "$version_listing" ] || printf '%s' "$version_listing" > "$fixtures/v${VERSION}.tsv"
  local actual=0
  FIXTURES="$fixtures" PATH="$work/bin:$PATH" GITHUB_STEP_SUMMARY="$fixtures/summary.md" VERIFY_DIGEST_RETRY_SECONDS=0 \
    bash "$SCRIPT" "$REPO" "$VERSION" "$ROLLING" "$ZIP" "$EXE" > "$fixtures/out.txt" 2>&1 || actual=$?
  if [ "$actual" -ne "$expected" ]; then
    echo "FAIL ${name}: exit ${actual}, expected ${expected}"; cat "$fixtures/out.txt"; failures=$((failures + 1)); return
  fi
  if ! grep -qF "$expected_text" "$fixtures/out.txt" || ! grep -qF "$expected_text" "$fixtures/summary.md"; then
    echo "FAIL ${name}: output or step summary lacks '${expected_text}'"; cat "$fixtures/out.txt"; failures=$((failures + 1)); return
  fi
  local rolling_reads
  rolling_reads="$(cat "$fixtures/${ROLLING}.reads" 2>/dev/null || echo 0)"
  if [ -n "$expected_rolling_reads" ] && [ "$rolling_reads" -ne "$expected_rolling_reads" ]; then
    echo "FAIL ${name}: ${ROLLING} read ${rolling_reads} times, expected ${expected_rolling_reads}"; failures=$((failures + 1)); return
  fi
  echo "ok   ${name}"
}

good_version="$(listing "v${VERSION}" "$ZIP" "$ZIP_SHA" "$EXE" "$EXE_SHA" stable.json 'sha256:cccc')"
good_rolling="$(listing "Office PC package ${VERSION}" "$ZIP" "$ZIP_SHA" "$EXE" "$EXE_SHA" \
  "klacks-office-pc-win-x64.previous.zip" "$OLD_SHA")"

run_case green "$EXIT_OK" "$good_rolling" "$good_version" "${ROLLING} serves ${VERSION}" "" "" "$FIRST_READ"
run_case newer-rolling "$EXIT_OK" "$(listing 'Office PC package 1.0.50' "$ZIP" "$OLD_SHA" "$EXE" "$OLD_SHA")" "" "newer than ${VERSION}"
run_case older-title "$EXIT_STALE" "$(listing 'Office PC package 1.0.48' "$ZIP" "$OLD_SHA" "$EXE" "$OLD_SHA")" "$good_version" "still serves 1.0.48"
run_case title-without-version "$EXIT_STALE" "$(listing 'Office PC package' "$ZIP" "$ZIP_SHA")" "$good_version" "names no version"
run_case stale-exe "$EXIT_STALE" "$(listing "Office PC package ${VERSION}" "$ZIP" "$ZIP_SHA" "$EXE" "$OLD_SHA")" "$good_version" "${EXE} on ${ROLLING}"
run_case stale-zip "$EXIT_STALE" "$(listing "Office PC package ${VERSION}" "$ZIP" "$OLD_SHA" "$EXE" "$EXE_SHA")" "$good_version" "${ZIP} on ${ROLLING}"
run_case exe-missing-on-rolling "$EXIT_STALE" "$(listing "Office PC package ${VERSION}" "$ZIP" "$ZIP_SHA")" "$good_version" "${EXE} is missing on ${ROLLING}"
run_case exe-missing-on-version "$EXIT_STALE" "$good_rolling" "$(listing "v${VERSION}" "$ZIP" "$ZIP_SHA")" "${EXE} is missing on v${VERSION}"
exe_without_digest="$(listing "Office PC package ${VERSION}" "$ZIP" "$ZIP_SHA" "$EXE" "")"
run_case digest-missing-for-good "$EXIT_STALE" "$exe_without_digest" "$good_version" "has no SHA-256 digest" \
  "" "" "$DIGEST_READ_ATTEMPTS"
run_case digest-arrives-on-second-read "$EXIT_OK" "$exe_without_digest" "$good_version" "${ROLLING} serves ${VERSION}" \
  "$good_rolling" "$SECOND_READ" "$SECOND_READ"
run_case version-release-unreadable "$EXIT_STALE" "$good_rolling" "" "Reading release v${VERSION}"
run_case rolling-release-unreadable "$EXIT_STALE" "" "" "Reading release ${ROLLING}"

if [ "$failures" -gt 0 ]; then
  echo "${failures} case(s) failed"
  exit 1
fi
echo "All cases passed"
