#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Builds the block artifacts.postgres of stable.json from deploy/postgres/engine.json:
#   { major, version, pgvector, docker: { image, sha256, signature }, native: [ { rid, bundleUrl, sha256, signature } ] }
# The Docker entry is signed here: RSA-SHA256/PKCS#1 over "klacks-postgres-engine\n{major}\n{image}\n{sha256}" (UTF-8, no
# trailing newline), with the OCI index digest of the image as sha256. The native entry is fetched from the engine
# prerelease (postgres-engine-<version>-pgvector<pgvector><releaseSuffix>) and only adopted when it names the expected RID
# and URL, its ZIP downloads with exactly that SHA-256 and its signature verifies; anything else drops the native entry
# with a warning (the Docker entry stays). A Docker entry that cannot be built is an error and no output file is written.
#
# Usage: New-EngineManifestBlock.sh --engine-json <file> --private-key <pem> --out <file>
#          [--docker-digest sha256:<hex>] [--release-base <url>] [--skip-native]
# Without --docker-digest the digest is read with "docker buildx imagetools inspect". The private key file is only read;
# the caller creates and deletes it. Needs bash, jq, openssl, curl, sha256sum (docker only without --docker-digest).

set -euo pipefail

readonly PAYLOAD_PREFIX='klacks-postgres-engine'
readonly DEFAULT_RELEASE_BASE='https://github.com/HeribertG/Klacks.Api/releases/download'
readonly NATIVE_RID='win-x64'
readonly CURL_MAX_SECONDS=900
readonly CURL_RETRIES=2
readonly CURL_MAX_BYTES=1073741824

engine_json=""
private_key=""
out=""
docker_digest=""
release_base="$DEFAULT_RELEASE_BASE"
skip_native=0

die() { echo "::error::$*" >&2; exit 1; }
warn() { echo "::warning::$*" >&2; }

while [ $# -gt 0 ]; do
  case "$1" in
    --engine-json) engine_json="${2:?}"; shift 2 ;;
    --private-key) private_key="${2:?}"; shift 2 ;;
    --out) out="${2:?}"; shift 2 ;;
    --docker-digest) docker_digest="${2:?}"; shift 2 ;;
    --release-base) release_base="${2:?}"; shift 2 ;;
    --skip-native) skip_native=1; shift ;;
    *) die "Unknown argument '$1'" ;;
  esac
done
[ -f "$engine_json" ] || die "--engine-json file not found"
[ -f "$private_key" ] || die "--private-key file not found"
[ -n "$out" ] || die "--out is required"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

major="$(jq -er '.major' "$engine_json")"
version="$(jq -er '.version' "$engine_json")"
pgvector="$(jq -er '.pgvector' "$engine_json")"
image="$(jq -er '.image' "$engine_json")"
suffix="$(jq -r '.releaseSuffix // ""' "$engine_json")"

[[ "$major" =~ ^[0-9]+$ ]] || die "engine.json major '$major' is not a number"
[[ "$version" =~ ^[0-9]+\.[0-9]+$ ]] || die "engine.json version '$version' is not <major>.<minor>"
[ "${version%%.*}" = "$major" ] || die "engine.json version '$version' does not belong to major $major"
[[ "$pgvector" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || die "engine.json pgvector '$pgvector' is not X.Y.Z"
[[ "$image" =~ ^ghcr\.io/[a-z0-9._/-]+:$major$ ]] || die "engine.json image '$image' is not a ghcr.io image tagged :$major"
[[ "$suffix" =~ ^(-r[0-9]+)?$ ]] || die "engine.json releaseSuffix '$suffix' is not empty or -rN"

openssl pkey -in "$private_key" -pubout -out "$work/public.pem" 2>/dev/null || die "Cannot derive the public key from --private-key"

payload() { printf '%s\n%s\n%s\n%s' "$PAYLOAD_PREFIX" "$1" "$2" "$3"; }

sign() { payload "$1" "$2" "$3" | openssl dgst -sha256 -sign "$private_key" | base64 -w0; }

verify() {
  printf '%s' "$4" | base64 -d > "$work/signature.bin" 2>/dev/null || return 1
  payload "$1" "$2" "$3" > "$work/payload.bin"
  openssl dgst -sha256 -verify "$work/public.pem" -signature "$work/signature.bin" "$work/payload.bin" > /dev/null 2>&1
}

if [ -z "$docker_digest" ]; then
  docker_digest="$(docker buildx imagetools inspect "$image" --format '{{.Manifest.Digest}}')" \
    || die "Could not read the digest of $image"
fi
[[ "$docker_digest" =~ ^sha256:[0-9a-f]{64}$ ]] || die "Docker digest '$docker_digest' is not sha256:<64 lowercase hex>"

docker_signature="$(sign "$major" "$image" "$docker_digest")"
verify "$major" "$image" "$docker_digest" "$docker_signature" || die "The fresh Docker signature does not verify"

native='[]'
if [ "$skip_native" -eq 0 ]; then
  tag="postgres-engine-${version}-pgvector${pgvector}${suffix}"
  zip_name="klacks-postgres-${version}-${NATIVE_RID}.zip"
  entry_name="klacks-postgres-${version}-${NATIVE_RID}.json"
  expected_url="${release_base}/${tag}/${zip_name}"
  fetch() { curl --fail --silent --show-error --location --proto-redir "=https,http" --max-filesize "$CURL_MAX_BYTES" --max-time "$CURL_MAX_SECONDS" --retry "$CURL_RETRIES" -o "$2" "$1"; }

  native_error=""
  if ! fetch "${release_base}/${tag}/${entry_name}" "$work/native-entry.json" 2> "$work/curl.err"; then
    native_error="entry not available ($(head -c 200 "$work/curl.err"))"
  elif ! jq -e 'type == "object"' "$work/native-entry.json" > /dev/null 2>&1; then
    native_error="entry is not a JSON object"
  else
    n_rid="$(jq -r '.rid // ""' "$work/native-entry.json")"
    n_url="$(jq -r '.bundleUrl // ""' "$work/native-entry.json")"
    n_sha="$(jq -r '.sha256 // ""' "$work/native-entry.json")"
    n_sig="$(jq -r '.signature // ""' "$work/native-entry.json")"
    if [ "$n_rid" != "$NATIVE_RID" ]; then native_error="entry RID '$n_rid' is not '$NATIVE_RID'"
    elif [ "$n_url" != "$expected_url" ]; then native_error="entry URL '$n_url' is not '$expected_url'"
    elif [[ ! "$n_sha" =~ ^[0-9a-f]{64}$ ]]; then native_error="entry sha256 is not 64 lowercase hex characters"
    elif ! verify "$major" "$n_url" "$n_sha" "$n_sig"; then native_error="entry signature does not verify"
    elif ! fetch "$n_url" "$work/native.zip" 2> "$work/curl.err"; then native_error="ZIP not available ($(head -c 200 "$work/curl.err"))"
    elif [ "$(sha256sum "$work/native.zip" | cut -d' ' -f1)" != "$n_sha" ]; then native_error="downloaded ZIP does not have the SHA-256 of the entry"
    fi
  fi

  if [ -n "$native_error" ]; then
    warn "Native engine entry dropped: $native_error"
  else
    native="$(jq -c '[{rid, bundleUrl, sha256, signature}]' "$work/native-entry.json")"
  fi
fi

jq -n --argjson major "$major" --arg version "$version" --arg pgvector "$pgvector" --arg image "$image" \
  --arg dsha "$docker_digest" --arg dsig "$docker_signature" --argjson native "$native" \
  '{major: $major, version: $version, pgvector: $pgvector, docker: {image: $image, sha256: $dsha, signature: $dsig}, native: $native}' \
  > "$out.tmp"
mv "$out.tmp" "$out"
echo "Engine block written to $out (docker entry signed, native entries: $(jq '.native | length' "$out"))"
