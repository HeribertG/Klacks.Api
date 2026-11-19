#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Runner-side driver of .github/workflows/hetzner-engine-upgrade.yml: uploads hetzner-engine-upgrade.sh, the enqueue helper and
# the candidate server compose file to the production server with plain ssh (no third-party action sees the key), starts the
# runbook detached there (setsid, so a dropped connection neither kills an upgrade in progress nor leaves the reset cron
# suspended half-way) and follows its log until it ends. The exit code of the remote run becomes the exit code of this script;
# a connection that stays down is reported as an error, the remote run may then still be active.
#
# Usage: hetzner-engine-upgrade-remote.sh --mode <mode> [--confirm <word>] [--pre-release]
# Environment: HEU_HOST, HEU_USER, HEU_PORT (22), HEU_KEY_FILE (private key file, mode 600), HEU_KNOWN_HOSTS (optional known_hosts
# content, else ssh-keyscan), HEU_RUN_ID (default GITHUB_RUN_ID), HEU_REPO_ROOT (default: this repository), HEU_REMOTE_BASE
# (/root/pg19-upgrade), HEU_MAX_WAIT_SECONDS (9000), HEU_POLL_SECONDS (20), HEU_MAX_SSH_FAILURES (20).
# Needs bash, ssh, ssh-keyscan (without HEU_KNOWN_HOSTS), tar.

set -euo pipefail

readonly TOKEN_PATTERN='^[A-Za-z0-9._-]+$'
readonly CONFIRM_PATTERN='^[A-Z0-9-]*$'
readonly SSH_ALIVE_INTERVAL=30
readonly SSH_ALIVE_COUNT=10
readonly SCAN_TIMEOUT_SECONDS=20

die() { echo "ERROR: $*" >&2; exit 1; }

mode=""
confirm=""
pre_release=0
while [ $# -gt 0 ]; do
  case "$1" in
    --mode) mode="${2:?}"; shift 2 ;;
    --confirm) confirm="${2:-}"; shift 2 ;;
    --pre-release) pre_release=1; shift ;;
    *) die "Unknown argument '$1'" ;;
  esac
done

case "$mode" in
  preflight | upgrade | rollback | regenerate-golden | cleanup) ;;
  *) die "--mode must be preflight, upgrade, rollback, regenerate-golden or cleanup" ;;
esac
[[ "$confirm" =~ $CONFIRM_PATTERN ]] || die "--confirm contains unexpected characters"

host="${HEU_HOST:?HEU_HOST is required}"
user="${HEU_USER:?HEU_USER is required}"
port="${HEU_PORT:-22}"
key_file="${HEU_KEY_FILE:?HEU_KEY_FILE is required}"
run_id="${HEU_RUN_ID:-${GITHUB_RUN_ID:-manual}}"
repo_root="${HEU_REPO_ROOT:-$(cd "$(dirname "$0")/../.." && pwd)}"
remote_base="${HEU_REMOTE_BASE:-/root/pg19-upgrade}"
max_wait="${HEU_MAX_WAIT_SECONDS:-9000}"
poll="${HEU_POLL_SECONDS:-20}"
max_failures="${HEU_MAX_SSH_FAILURES:-20}"

[[ "$host" =~ $TOKEN_PATTERN ]] || die "HEU_HOST contains unexpected characters"
[[ "$user" =~ $TOKEN_PATTERN ]] || die "HEU_USER contains unexpected characters"
[[ "$port" =~ ^[0-9]+$ ]] || die "HEU_PORT is not a number"
[[ "$run_id" =~ $TOKEN_PATTERN ]] || die "HEU_RUN_ID contains unexpected characters"
[ -f "$key_file" ] || die "HEU_KEY_FILE not found"
[ -f "$repo_root/deploy/scripts/hetzner-engine-upgrade.sh" ] || die "hetzner-engine-upgrade.sh not found below $repo_root"
[ -f "$repo_root/deploy/docker-compose-server.yml" ] || die "docker-compose-server.yml not found below $repo_root"

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

if [ -n "${HEU_KNOWN_HOSTS:-}" ]; then
  printf '%s\n' "$HEU_KNOWN_HOSTS" > "$work/known_hosts"
else
  ssh-keyscan -p "$port" -T "$SCAN_TIMEOUT_SECONDS" "$host" > "$work/known_hosts" 2> /dev/null || true
fi
[ -s "$work/known_hosts" ] || die "no host key for $host (set the SSH_KNOWN_HOSTS secret or check the host)"

ssh_remote() {
  ssh -i "$key_file" -p "$port" -o BatchMode=yes -o IdentitiesOnly=yes -o StrictHostKeyChecking=yes \
    -o UserKnownHostsFile="$work/known_hosts" -o ServerAliveInterval="$SSH_ALIVE_INTERVAL" -o ServerAliveCountMax="$SSH_ALIVE_COUNT" \
    "$user@$host" "$@"
}

rdir="$remote_base/run-$run_id"
files=(deploy/scripts/hetzner-engine-upgrade.sh deploy/docker-compose-server.yml)
if [ -f "$repo_root/deploy/postgres/enqueue-engine-upgrade.sh" ]; then
  files+=(deploy/postgres/enqueue-engine-upgrade.sh)
fi

echo "Uploading ${#files[@]} files to $rdir"
tar czf - -C "$repo_root" --transform 's|.*/||' "${files[@]}" | ssh_remote "umask 077 && mkdir -p '$remote_base' && mkdir '$rdir' && tar xzf - -C '$rdir'" || die "could not create $rdir (a run with this id already exists?)"

args="--mode $mode --run-id $run_id --compose $rdir/docker-compose-server.yml --enqueue $rdir/enqueue-engine-upgrade.sh"
[ -z "$confirm" ] || args="$args --confirm $confirm"
[ "$pre_release" -eq 0 ] || args="$args --pre-release"

ssh_remote 'bash -s' <<REMOTE
set -e
cd '$rdir'
rm -f run.exit run.log
setsid nohup bash -c 'bash ./hetzner-engine-upgrade.sh $args > run.log 2>&1; echo \$? > run.exit' > /dev/null 2>&1 < /dev/null &
echo "Started $mode on the server."
REMOTE

marker="$(od -An -N12 -tx1 /dev/urandom | tr -d ' \n')"
position=0
waited=0
failures=0
while [ "$waited" -lt "$max_wait" ]; do
  sleep "$poll"
  waited=$((waited + poll))
  if ! chunk="$(ssh_remote "cd '$rdir' && n=\$(wc -l < run.log) && tail -n +$((position + 1)) run.log | head -n \$((n - $position)) && echo \"@@POS-$marker \$n\" && if [ -f run.exit ]; then echo \"@@EXIT-$marker \$(cat run.exit)\"; fi" 2> /dev/null)"; then
    failures=$((failures + 1))
    echo "(connection to the server failed, attempt $failures of $max_failures; the remote run continues)"
    if [ "$failures" -ge "$max_failures" ]; then die "the server did not answer $max_failures times in a row; the remote run may still be active, check $rdir/run.log"; fi
    continue
  fi
  failures=0
  printf '%s\n' "$chunk" | grep -v -e "^@@POS-$marker " -e "^@@EXIT-$marker " | LC_ALL=C tr -d '\000-\010\013\014\016-\037' | sed -E 's/^[[:space:]]*::/: :/' || true
  new_position="$(printf '%s\n' "$chunk" | sed -n "s/^@@POS-$marker \([0-9][0-9]*\)\$/\1/p" | tail -n1)"
  [ -z "$new_position" ] || position=$((10#$new_position))
  exit_code="$(printf '%s\n' "$chunk" | sed -n "s/^@@EXIT-$marker \([0-9][0-9]*\)\$/\1/p" | tail -n1)"
  if [ -n "$exit_code" ]; then
    echo "Remote run finished with exit code $exit_code."
    exit "$exit_code"
  fi
done
die "the remote run did not finish within $((max_wait / 60)) minutes; it may still be active, check $rdir/run.log"
