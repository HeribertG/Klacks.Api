#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Engine upgrade gate: signs an engine upgrade row with a throw-away CI key and prints the Base64 signature.
# Payload (Klacks.Updater ArtifactSignaturePayload for operation type 4), UTF-8 without trailing newline:
#   klacks-postgres-engine\n<target major>\n<artifact ref>\n<artifact sha256>
# The signature is verified with the public key before it is printed, so a wrong key pair fails here and not in the
# updater.
#
# Usage: sign-engine-row.sh --key <private key PEM> --public-key <public key PEM> --to <major> --ref <ref> --sha <sha>
#
# Never pass the production signing key: the gates use a key pair generated inside the job.
set -euo pipefail

readonly SIGNATURE_DOMAIN='klacks-postgres-engine'

die() { printf 'sign-engine-row: %s\n' "$1" >&2; exit 2; }

private_key=''
public_key=''
target=''
artifact_ref=''
artifact_sha=''
while [ $# -gt 0 ]; do
  [ $# -ge 2 ] || die "missing value for $1"
  case "$1" in
    --key) private_key="$2" ;;
    --public-key) public_key="$2" ;;
    --to) target="$2" ;;
    --ref) artifact_ref="$2" ;;
    --sha) artifact_sha="$2" ;;
    *) die "unknown option $1" ;;
  esac
  shift 2
done
[ -f "$private_key" ] || die '--key must name the private key file.'
[ -f "$public_key" ] || die '--public-key must name the public key file.'
if [ -z "$target" ] || [ -z "$artifact_ref" ] || [ -z "$artifact_sha" ]; then die '--to, --ref and --sha are required.'; fi

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
printf '%s\n%s\n%s\n%s' "$SIGNATURE_DOMAIN" "$target" "$artifact_ref" "$artifact_sha" > "$work/payload"
openssl dgst -sha256 -sign "$private_key" -out "$work/signature" "$work/payload"
openssl dgst -sha256 -verify "$public_key" -signature "$work/signature" "$work/payload" >/dev/null \
  || die 'the signature does not verify with the public key.'
base64 "$work/signature" | tr -d '\r\n'
printf '\n'
