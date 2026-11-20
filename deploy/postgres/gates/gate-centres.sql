-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: the 100 cluster centres of dimension 768 as a temporary table gate_centres (session only).
-- Every component is a small integer from md5, so the centres are the same on every run and platform. Single source
-- for seed-gate-vectors.sql (members = centre + noise) and gate-probe-vectors.sql (probes = centre + noise).

DROP TABLE IF EXISTS pg_temp.gate_centres;
CREATE TEMP TABLE gate_centres AS
SELECT c AS cluster, j AS dim, (get_byte(decode(md5('c:' || c || ':' || j), 'hex'), 0) % 13) - 6 AS value
FROM generate_series(0, 99) AS c, generate_series(1, 768) AS j;
