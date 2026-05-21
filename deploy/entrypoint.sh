#!/bin/sh
# Copyright (c) Heribert Gasparoli Private. All rights reserved.

# Entrypoint that ensures writable mount points are owned by appuser before
# dropping privileges. Docker named volumes are root:root by default and
# override any ownership set on the image directory, so the Dockerfile's
# COPY --chown alone is not enough for volume-backed paths.

set -e

for dir in /app/Images /app/Documents /app/DataProtection-Keys /app/Cache/Models; do
    mkdir -p "$dir"
    chown -R appuser:appuser "$dir"
done

exec gosu appuser dotnet Klacks.Api.dll
