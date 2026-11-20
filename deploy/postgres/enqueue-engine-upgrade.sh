#!/usr/bin/env bash
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
#
# Queues a signed PostgreSQL engine upgrade (update_history row with operation_type 4) for the Klacks updater and
# prints the id of the new operation on stdout (nothing else goes to stdout).
#
# Usage:
#   enqueue-engine-upgrade.sh --psql "<command prefix>" --from 17 --to 19 --ref <artifact ref> --sha <sha> \
#                             --sig <base64 signature> --requested-by <who>
#
#   --psql          psql command prefix that reaches the Klacks database, for example
#                   "docker exec -i klacks-postgres psql -U admin -d Klacks" or
#                   "'/c/Program Files/Klacks/pgsql/bin/psql.exe' -h 127.0.0.1 -p 5439 -U klacks -d Klacks"
#                   (password through PGPASSWORD). The prefix is evaluated by the shell, so quote paths inside it.
#                   The script appends its own psql options and feeds the SQL on stdin.
#   --from / --to   source and target PostgreSQL major (from_version / target_version of the row)
#   --ref           artifact_ref: the image reference (Docker) or the https URL of the engine bundle (Windows)
#   --sha           artifact_sha256: sha256:<64 hex> (Docker image index digest) or 64 lowercase hex (bundle)
#   --sig           artifact_signature: Base64 RSA-SHA256 signature over
#                   "klacks-postgres-engine\n<to>\n<ref>\n<sha>" (UTF-8, no trailing newline)
#   --requested-by  who queued it (audit; letters, digits and . _ @ : - only)
#
# Exit codes: 0 = queued (id printed), 2 = invalid arguments, 3 = another operation is pending or running (nothing
# queued), 4 = psql failed. The single-active partial unique index of update_history backs the active check, so a
# concurrent insert also ends with exit code 4 instead of a second active row.
set -euo pipefail
# Byte-wise character classes in the patterns below, whatever locale the caller runs in.
export LC_ALL=C

readonly OPERATION_TYPE_ENGINE_UPGRADE=4
readonly STATUS_PENDING=0
readonly STATUS_RUNNING=1
readonly CHANNEL_STABLE=0
readonly EXIT_USAGE=2
readonly EXIT_ACTIVE=3
readonly EXIT_PSQL=4
readonly ACTIVE_MARKER='ACTIVE'
readonly UUID_PATTERN='^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
readonly MAJOR_PATTERN='^[1-9][0-9]{0,2}$'
readonly REF_PATTERN='^[A-Za-z0-9][A-Za-z0-9._/:@+-]*$'
readonly REF_MAX_LENGTH=512
readonly SHA_PATTERN='^(sha256:)?[0-9a-f]{64}$'
readonly SIGNATURE_PATTERN='^[A-Za-z0-9+/]+={0,2}$'
readonly SIGNATURE_MAX_LENGTH=8192
readonly REQUESTED_BY_PATTERN='^[A-Za-z0-9._@:-]{1,128}$'

usage() {
  echo 'Usage: enqueue-engine-upgrade.sh --psql "<command prefix>" --from <major> --to <major> --ref <ref> --sha <sha> --sig <base64> --requested-by <who> (details in the header)' >&2
  exit "$EXIT_USAGE"
}

die() { printf 'enqueue-engine-upgrade: %s\n' "$1" >&2; exit "${2:-$EXIT_USAGE}"; }

# Caller input in messages: every non-printable character replaced, so it can never start a log command.
printable() { printf "%s" "${1//[^[:print:]]/?}"; }

psql_prefix=''
from_major=''
to_major=''
artifact_ref=''
artifact_sha=''
artifact_signature=''
requested_by=''

while [ $# -gt 0 ]; do
  [ $# -ge 2 ] || usage
  case "$1" in
    --psql) psql_prefix="$2" ;;
    --from) from_major="$2" ;;
    --to) to_major="$2" ;;
    --ref) artifact_ref="$2" ;;
    --sha) artifact_sha="$2" ;;
    --sig) artifact_signature="$2" ;;
    --requested-by) requested_by="$2" ;;
    *) usage ;;
  esac
  shift 2
done

[ -n "$psql_prefix" ] || die '--psql is required.'
[[ "$from_major" =~ $MAJOR_PATTERN ]] || die "--from '$(printable "$from_major")' is not a PostgreSQL major."
[[ "$to_major" =~ $MAJOR_PATTERN ]] || die "--to '$(printable "$to_major")' is not a PostgreSQL major."
[ "$to_major" -gt "$from_major" ] || die "--to $to_major must be newer than --from $from_major."
# Strict formats first (no quotes, backslashes, whitespace or control characters); the SQL binds them as psql variables.
if ! [[ "$artifact_ref" =~ $REF_PATTERN ]] || [ "${#artifact_ref}" -gt "$REF_MAX_LENGTH" ]; then
  die "--ref is empty, longer than $REF_MAX_LENGTH characters or contains characters outside [A-Za-z0-9._/:@+-]."
fi
[[ "$artifact_sha" =~ $SHA_PATTERN ]] || die '--sha must be sha256:<64 hex> or 64 lowercase hex characters.'
if ! [[ "$artifact_signature" =~ $SIGNATURE_PATTERN ]] || [ "${#artifact_signature}" -gt "$SIGNATURE_MAX_LENGTH" ]; then
  die '--sig is not a Base64 signature.'
fi
[[ "$requested_by" =~ $REQUESTED_BY_PATTERN ]] || die "--requested-by '$(printable "$requested_by")' contains characters outside [A-Za-z0-9._@:-]."

# The values travel as psql variables and reach the statement only through :'name' (quoted literal), never by string
# concatenation. psql interpolates variables in a script read from stdin (not in -c), hence the SQL goes on stdin.
# One statement: the row is only inserted when no operation is pending or running; otherwise the marker comes back.
sql="WITH active AS (
  SELECT 1 FROM update_history WHERE status IN ($STATUS_PENDING, $STATUS_RUNNING) AND is_deleted = false
), inserted AS (
  INSERT INTO update_history (
    id, operation_type, status, channel, from_version, target_version, artifact_ref, artifact_sha256,
    artifact_signature, contains_migrations, requested_by, requested_at, create_time, current_user_created, is_deleted)
  SELECT gen_random_uuid(), $OPERATION_TYPE_ENGINE_UPGRADE, $STATUS_PENDING, $CHANNEL_STABLE, :'from_major', :'to_major',
    :'artifact_ref', :'artifact_sha', :'artifact_signature', false, :'requested_by', now(), now(), :'requested_by', false
  WHERE NOT EXISTS (SELECT 1 FROM active)
  RETURNING id
)
SELECT coalesce((SELECT id::text FROM inserted), '$ACTIVE_MARKER');"

# printf %q keeps every value one shell word when the prefix is evaluated.
psql_variables=''
for assignment in "from_major=$from_major" "to_major=$to_major" "artifact_ref=$artifact_ref" "artifact_sha=$artifact_sha" \
                  "artifact_signature=$artifact_signature" "requested_by=$requested_by"; do
  psql_variables="$psql_variables -v $(printf '%q' "$assignment")"
done

if ! result="$(printf '%s\n' "$sql" | eval "$psql_prefix -X -q -v ON_ERROR_STOP=1 -At $psql_variables")"; then
  die 'psql failed; nothing was queued (an operation queued at the same moment also ends here).' "$EXIT_PSQL"
fi
result="$(printf '%s' "$result" | tr -d '\r' | tail -n 1)"

if [ "$result" = "$ACTIVE_MARKER" ]; then
  die 'another update operation is pending or running; nothing was queued.' "$EXIT_ACTIVE"
fi
[[ "$result" =~ $UUID_PATTERN ]] || die "unexpected psql output '$(printable "$result")'." "$EXIT_PSQL"
printf '%s\n' "$result"
