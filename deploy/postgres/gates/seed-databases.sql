-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: creates the two extra databases the production server also carries (klacks_diag,
-- klacks_telemetry), owned by the connecting bootstrap superuser, so the upgrade has to migrate more than one
-- database. Run with psql against the Klacks database; idempotent. No role is created: the upgrade refuses
-- clusters with roles other than the bootstrap superuser.

SELECT format('CREATE DATABASE %I TEMPLATE template0 ENCODING %L', name, 'UTF8')
FROM (VALUES ('klacks_diag'), ('klacks_telemetry')) AS wanted(name)
WHERE NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = wanted.name)
\gexec
