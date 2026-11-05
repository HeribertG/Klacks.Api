#!/bin/sh
# Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

# Entrypoint that ensures writable mount points are owned by appuser before
# dropping privileges. Docker named volumes are root:root by default and
# override any ownership set on the image directory, so the Dockerfile's
# COPY --chown alone is not enough for volume-backed paths.
#
# /app/ErpImport is a host folder (bind mount), not a named volume: an ERP on
# the host writes into it under its own account. A recursive chown there would
# take ownership of the ERP's files and undo the administrator's group setup on
# every start, so the folder is only created and, if appuser cannot write into
# it AND Docker just created it as root:root, handed to appuser non-recursively
# with mode 2770 (setgid, no access for other host users; the group is kept).
# A folder the administrator prepared (other owner or group) is never changed.
# Failing to do so (Docker Desktop on Windows, rootless Docker, NFS) is not
# fatal: the API starts and the folder-health check reports the problem.

set -e

for dir in /app/Images /app/Documents /app/DataProtection-Keys /app/Cache/Models /app/PayrollExports; do
    mkdir -p "$dir"
    chown -R appuser:appuser "$dir"
done

ERP_IMPORT_DIR=/app/ErpImport
mkdir -p "$ERP_IMPORT_DIR" || true
if ! gosu appuser test -w "$ERP_IMPORT_DIR"; then
    if [ "$(stat -c '%u:%g' "$ERP_IMPORT_DIR" 2>/dev/null)" = "0:0" ] && chown appuser "$ERP_IMPORT_DIR" && chmod 2770 "$ERP_IMPORT_DIR"; then
        echo "entrypoint: $ERP_IMPORT_DIR was created as root:root, now owned by appuser (non-recursive, mode 2770)"
    fi
    if ! gosu appuser test -w "$ERP_IMPORT_DIR"; then
        echo "entrypoint: WARNING $ERP_IMPORT_DIR is not writable for appuser (uid $(id -u appuser)); the ERP folder import will fail until the host folder grants write access" >&2
    fi
fi

exec gosu appuser dotnet Klacks.Api.dll
