#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Places the signed Windows setup of one release on the store server exactly once, as the package URL of the Microsoft
# Store listing: <root>/windows/<version>/<file name>, served by Klacks.Marketplace at
# <public base>/windows/<version>/<file name> without a redirect. The Microsoft Store rejects a submission whose binary
# changes behind its URL, so an existing file is never replaced: same SHA-256 = nothing to do, different SHA-256 = error.
# The upload lands in a temporary name first and is moved into place with mv -n (atomic on the same file system).
# Afterwards the public URL is downloaded and compared byte for byte.
#
# Usage: publish-store-download.sh <exe> <version>
# Environment: STORE_SSH_HOST, STORE_SSH_USER, STORE_SSH_KEY_FILE, STORE_SSH_PORT (default 22),
#              STORE_DOWNLOAD_ROOT (server folder, e.g. /root/apps/downloads),
#              STORE_PUBLIC_BASE_URL (e.g. https://klacks-software.ch/store/api/downloads),
#              STORE_VERIFY_ATTEMPTS / STORE_VERIFY_RETRY_SECONDS (optional), GITHUB_STEP_SUMMARY (optional).
# Exit codes: 0 = in place and publicly identical, 1 = different file already published or public check failed,
#             2 = usage error.

set -euo pipefail

readonly EXIT_FAILED=1
readonly EXIT_USAGE=2
readonly VERSION_PATTERN='^[0-9]{1,4}\.[0-9]{1,4}\.[0-9]{1,4}$'
readonly PLATFORM_FOLDER='windows'
readonly DEFAULT_SSH_PORT=22
readonly DEFAULT_VERIFY_ATTEMPTS=6
readonly DEFAULT_VERIFY_RETRY_SECONDS=20
readonly VERIFY_ATTEMPTS="${STORE_VERIFY_ATTEMPTS:-$DEFAULT_VERIFY_ATTEMPTS}"
readonly VERIFY_RETRY_SECONDS="${STORE_VERIFY_RETRY_SECONDS:-$DEFAULT_VERIFY_RETRY_SECONDS}"
readonly CURL_CONNECT_TIMEOUT_SECONDS=20
readonly CURL_MAX_TIME_SECONDS=900

if [ "$#" -ne 2 ]; then
  echo "Usage: $0 <exe> <version>" >&2
  exit "$EXIT_USAGE"
fi

exe="$1"
version="$2"
if [ ! -f "$exe" ]; then
  echo "::error::Setup file '$exe' does not exist." >&2
  exit "$EXIT_USAGE"
fi
if ! [[ "$version" =~ $VERSION_PATTERN ]]; then
  echo "::error::Version '$version' is not X.Y.Z." >&2
  exit "$EXIT_USAGE"
fi
for name in STORE_SSH_HOST STORE_SSH_USER STORE_SSH_KEY_FILE STORE_DOWNLOAD_ROOT STORE_PUBLIC_BASE_URL; do
  if [ -z "${!name:-}" ]; then
    echo "::error::$name is not set." >&2
    exit "$EXIT_USAGE"
  fi
done

file_name="$(basename "$exe")"
sha="$(sha256sum "$exe" | cut -d' ' -f1)"
port="${STORE_SSH_PORT:-$DEFAULT_SSH_PORT}"
target_dir="${STORE_DOWNLOAD_ROOT}/${PLATFORM_FOLDER}/${version}"
incoming="${STORE_DOWNLOAD_ROOT}/${PLATFORM_FOLDER}/.incoming-${version}-$$-${RANDOM}"
ssh_options=(-i "$STORE_SSH_KEY_FILE" -o BatchMode=yes -o StrictHostKeyChecking=accept-new)
remote="${STORE_SSH_USER}@${STORE_SSH_HOST}"
download=""

# Removes a left-over upload (failed scp or remote step) and the local verify download; best effort.
cleanup() {
  ssh "${ssh_options[@]}" -p "$port" "$remote" "rm -f $(printf '%q' "$incoming")" >/dev/null 2>&1 || true
  if [ -n "$download" ]; then rm -f "$download"; fi
}
trap cleanup EXIT

echo "Publishing ${file_name} ${version} (sha256 ${sha}) to ${target_dir}"
ssh "${ssh_options[@]}" -p "$port" "$remote" \
  "mkdir -p $(printf '%q' "${STORE_DOWNLOAD_ROOT}/${PLATFORM_FOLDER}")"
scp "${ssh_options[@]}" -P "$port" -q "$exe" "${remote}:${incoming}"

# Runs on the server. Prints PLACED, PRESENT or CONFLICT <sha>; the incoming file is always removed or moved.
set +e
remote_args="$(printf '%q ' "$incoming" "$target_dir" "$file_name" "$sha")"
result="$(ssh "${ssh_options[@]}" -p "$port" "$remote" "bash -s -- ${remote_args}" <<'REMOTE'
set -euo pipefail
incoming="$1"; target_dir="$2"; file_name="$3"; expected="$4"
target="${target_dir}/${file_name}"
actual_incoming="$(sha256sum "$incoming" | cut -d' ' -f1)"
if [ "$actual_incoming" != "$expected" ]; then
  rm -f "$incoming"
  echo "UPLOAD_CORRUPT ${actual_incoming}"
  exit 0
fi
mkdir -p "$target_dir"
chmod 0755 "$target_dir"
if [ -e "$target" ]; then
  rm -f "$incoming"
  present="$(sha256sum "$target" | cut -d' ' -f1)"
  if [ "$present" = "$expected" ]; then echo "PRESENT"; else echo "CONFLICT ${present}"; fi
  exit 0
fi
chmod 0444 "$incoming"
mv -n "$incoming" "$target"
rm -f "$incoming"
placed="$(sha256sum "$target" | cut -d' ' -f1)"
if [ "$placed" = "$expected" ]; then echo "PLACED"; else echo "CONFLICT ${placed}"; fi
REMOTE
)"
status=$?
set -e
if [ "$status" -ne 0 ]; then
  echo "::error::Remote placement failed (ssh exit ${status})." >&2
  exit "$EXIT_FAILED"
fi

case "$result" in
  PLACED) echo "Placed ${target_dir}/${file_name}." ;;
  PRESENT) echo "${target_dir}/${file_name} already holds this exact file; nothing changed." ;;
  CONFLICT*)
    echo "::error::${target_dir}/${file_name} already holds a DIFFERENT file (sha256 ${result#CONFLICT }). It is never replaced: the Microsoft Store would reject or pull the listing. Investigate by hand." >&2
    exit "$EXIT_FAILED" ;;
  UPLOAD_CORRUPT*)
    echo "::error::The upload arrived with sha256 ${result#UPLOAD_CORRUPT }, expected ${sha}. Nothing was placed; re-run the job." >&2
    exit "$EXIT_FAILED" ;;
  *)
    echo "::error::Unexpected answer from the server: '${result}'." >&2
    exit "$EXIT_FAILED" ;;
esac

public_url="${STORE_PUBLIC_BASE_URL%/}/${PLATFORM_FOLDER}/${version}/${file_name}"
download="$(mktemp)"
for attempt in $(seq 1 "$VERIFY_ATTEMPTS"); do
  http_code="$(curl -sS -o "$download" -w '%{http_code}' --max-redirs 0 \
    --connect-timeout "$CURL_CONNECT_TIMEOUT_SECONDS" --max-time "$CURL_MAX_TIME_SECONDS" "$public_url" || true)"
  if [ "$http_code" = "200" ] && [ "$(sha256sum "$download" | cut -d' ' -f1)" = "$sha" ]; then
    echo "Public URL serves the same file without redirect: ${public_url}"
    if [ -n "${GITHUB_STEP_SUMMARY:-}" ]; then
      {
        echo "### Microsoft Store package URL"
        echo ""
        echo "\`${public_url}\` (sha256 \`${sha}\`, ${result})"
      } >> "$GITHUB_STEP_SUMMARY"
    fi
    exit 0
  fi
  echo "Attempt ${attempt}/${VERIFY_ATTEMPTS}: ${public_url} answered HTTP ${http_code} or a different file."
  if [ "$attempt" -lt "$VERIFY_ATTEMPTS" ]; then sleep "$VERIFY_RETRY_SECONDS"; fi
done

echo "::error::${public_url} does not serve the published file without redirect (is Klacks.Marketplace deployed with the /app/downloads mount?). The file stays in place; re-run this job after fixing the route." >&2
exit "$EXIT_FAILED"
