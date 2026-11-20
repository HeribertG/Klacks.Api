-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: content of the extra databases klacks_diag and klacks_telemetry. engine_gate_rows holds 1000
-- deterministic rows; engine_gate_filler holds :filler_rows rows with an index, so dump and restore take long enough
-- for the kill-during-restore scenario to hit the restore phase (pass -v filler_rows=0 to skip it). Run with psql
-- against each extra database; recreates both tables on every run.

\set ON_ERROR_STOP on

DROP TABLE IF EXISTS engine_gate_rows;
CREATE TABLE engine_gate_rows (id integer PRIMARY KEY, label text NOT NULL, created date NOT NULL);
INSERT INTO engine_gate_rows (id, label, created)
SELECT i, md5('row:' || i), date '2026-01-01' + (i % 365)
FROM generate_series(1, 1000) AS i;

DROP TABLE IF EXISTS engine_gate_filler;
CREATE TABLE engine_gate_filler (id bigint PRIMARY KEY, payload text NOT NULL);
INSERT INTO engine_gate_filler (id, payload)
SELECT i, md5('filler:' || i) || md5('filler2:' || i) || md5('filler3:' || i)
FROM generate_series(1, :filler_rows) AS i;
CREATE INDEX engine_gate_filler_payload_idx ON engine_gate_filler (payload);

ANALYZE engine_gate_rows;
ANALYZE engine_gate_filler;
