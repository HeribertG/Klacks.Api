#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Shared by .github/workflows/deploy.yml and reset-and-deploy.yml; sourced on the server from the checked-out
# Klacks.Api repository, with the working directory /root/apps (where .env and docker-compose.yml live).
#
# whisper-stt and klacks-stt are profile-gated in the compose file (one local STT engine at a time). A host that
# already runs one of the containers must keep the matching profile in COMPOSE_PROFILES of .env, otherwise compose
# stops managing it (reset-and-deploy would take it down and never bring it back). Idempotent and additive only:
# profiles are never removed, other entries and lines are kept. Mirrors the comma-list semantics of
# Klacks.Updater ComposeEnvFile.WithProfile. Quoted values (COMPOSE_PROFILES="whisper") are read and rewritten
# unquoted. Must run before the first compose command that can stop or recreate containers; callers treat a failure
# as non-fatal because it only reads docker state and edits .env.

ensure_compose_profile_for_running_container() {
  local container="$1" profile="$2" env_file=".env"
  if [ -z "$(docker ps --filter "name=^${container}\$" --filter "status=running" --quiet 2>/dev/null)" ]; then
    echo "ℹ️  ${container} is not running - COMPOSE_PROFILES left untouched"
    return 0
  fi
  touch "$env_file"
  local profiles_line value tokens
  profiles_line=$(grep -m1 -E '^[[:space:]]*COMPOSE_PROFILES=' "$env_file" || true)
  if [ -z "$profiles_line" ]; then
    if [ -s "$env_file" ] && [ -n "$(tail -c1 "$env_file")" ]; then echo >> "$env_file"; fi
    echo "COMPOSE_PROFILES=${profile}" >> "$env_file"
    echo "✅ ${container} is running: added COMPOSE_PROFILES=${profile} to ${env_file}"
    return 0
  fi
  value="${profiles_line#"${profiles_line%%[![:space:]]*}"}"
  value="${value#COMPOSE_PROFILES=}"
  value="${value%$'\r'}"
  value="${value#"${value%%[![:space:]]*}"}"
  case "$value" in
    \"*) value="${value#\"}"; value="${value%%\"*}" ;;
    \'*) value="${value#\'}"; value="${value%%\'*}" ;;
    *) value="${value%%[[:space:]]#*}" ;;
  esac
  tokens=$(printf '%s' "$value" | tr ',' '\n' | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' -e '/^$/d' | paste -sd, -)
  case ",${tokens,,}," in
    *",${profile},"*)
      echo "ℹ️  ${container} is running and COMPOSE_PROFILES already contains ${profile}"
      return 0
      ;;
  esac
  tokens="${tokens:+${tokens},}${profile}"
  local tmp_file
  tmp_file=$( umask 077; mktemp "${env_file}.profiles.XXXXXX" )
  awk -v line="COMPOSE_PROFILES=${tokens}" 'BEGIN { done = 0 } !done && /^[ \t]*COMPOSE_PROFILES=/ { print line; done = 1; next } { print }' "$env_file" > "$tmp_file"
  cat "$tmp_file" > "$env_file"
  rm -f "$tmp_file"
  echo "✅ ${container} is running: COMPOSE_PROFILES extended to ${tokens}"
}

ensure_local_stt_compose_profiles() {
  ensure_compose_profile_for_running_container klacks-whisper-stt whisper \
    || echo "⚠️  Could not ensure COMPOSE_PROFILES for klacks-whisper-stt (non-fatal)"
  ensure_compose_profile_for_running_container klacks-stt parakeet \
    || echo "⚠️  Could not ensure COMPOSE_PROFILES for klacks-stt (non-fatal)"
}
