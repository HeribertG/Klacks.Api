-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: builds the 20 probe vectors as a temporary table gate_probes (session only, nothing is stored).
-- Probe p is the centre of cluster (7 * p) % 100 plus its own md5 noise, built exactly like the members in
-- seed-gate-vectors.sql. Included by snapshot-knn.sql and assert-hnsw.sql through \ir.

\ir gate-centres.sql

DROP TABLE IF EXISTS pg_temp.gate_probes;
CREATE TEMP TABLE gate_probes AS
SELECT probe.p,
       ('[' || string_agg((centre.value + (get_byte(decode(md5('p:' || probe.p || ':' || centre.dim), 'hex'), 0) % 5) - 2)::text,
                          ',' ORDER BY centre.dim) || ']')::vector(768) AS embedding
FROM (SELECT p, (7 * p) % 100 AS cluster FROM generate_series(1, 20) AS p) AS probe
JOIN gate_centres AS centre ON centre.cluster = probe.cluster
GROUP BY probe.p;
