-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: exact top-10 neighbours (cosine distance, ties by id) of the 20 probes in engine_gate_vectors.
-- Index scans are switched off, so the lists come from a sequential scan and never from the HNSW index. Output (psql
-- -At): one line "knn|<probe>|<id>,<id>,..." per probe (compared before/after the upgrade) and one line
-- "knnd|<probe>|<distance>,..." with the distances (log only). Run with psql against the Klacks database.

\set ON_ERROR_STOP on
\set QUIET on
\ir gate-probe-vectors.sql

SET enable_indexscan = off;
SET enable_bitmapscan = off;
SET enable_indexonlyscan = off;

SELECT 'knn|' || probe.p || '|' || string_agg(neighbour.id::text, ',' ORDER BY neighbour.distance, neighbour.id)
FROM gate_probes AS probe
CROSS JOIN LATERAL (
    SELECT v.id, v.embedding <=> probe.embedding AS distance
    FROM engine_gate_vectors AS v
    ORDER BY v.embedding <=> probe.embedding, v.id
    LIMIT 10
) AS neighbour
GROUP BY probe.p
ORDER BY probe.p;

SELECT 'knnd|' || probe.p || '|' || string_agg(to_char(neighbour.distance, 'FM0.000000000'), ',' ORDER BY neighbour.distance, neighbour.id)
FROM gate_probes AS probe
CROSS JOIN LATERAL (
    SELECT v.id, v.embedding <=> probe.embedding AS distance
    FROM engine_gate_vectors AS v
    ORDER BY v.embedding <=> probe.embedding, v.id
    LIMIT 10
) AS neighbour
GROUP BY probe.p
ORDER BY probe.p;
