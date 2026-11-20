-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: table engine_gate_vectors in the Klacks database with 2000 deterministic vectors of
-- dimension 768 and an HNSW index (vector_cosine_ops, like knowledge_index_embedding_idx). Run with psql against the
-- Klacks database; recreates the table on every run.
--
-- The vectors are 100 clusters of 20 members: member = cluster centre + small noise. Every component is a small
-- integer taken from md5 (no random()), so the data is identical on every run and platform, and every dot product
-- and norm pgvector computes is exact in float arithmetic; the cosine distances, and with them the exact top-10 lists
-- (ORDER BY distance, id), therefore come out bit-identical on PostgreSQL 17 and 19 and on any pgvector build.
-- The clusters keep the true neighbours of a probe well apart from the rest, so HNSW recall is meaningful.
-- gate-probe-vectors.sql builds the 20 probes from the same centres (both include gate-centres.sql).

\set ON_ERROR_STOP on

CREATE EXTENSION IF NOT EXISTS vector;

DROP TABLE IF EXISTS engine_gate_vectors;
CREATE TABLE engine_gate_vectors (
    id integer PRIMARY KEY,
    cluster integer NOT NULL,
    embedding vector(768) NOT NULL
);

\ir gate-centres.sql

INSERT INTO engine_gate_vectors (id, cluster, embedding)
SELECT member.id,
       member.cluster,
       ('[' || string_agg((centre.value + (get_byte(decode(md5('n:' || member.id || ':' || centre.dim), 'hex'), 0) % 5) - 2)::text,
                          ',' ORDER BY centre.dim) || ']')::vector(768)
FROM (SELECT i AS id, (i - 1) / 20 AS cluster FROM generate_series(1, 2000) AS i) AS member
JOIN gate_centres AS centre ON centre.cluster = member.cluster
GROUP BY member.id, member.cluster;

CREATE INDEX engine_gate_vectors_embedding_hnsw ON engine_gate_vectors USING hnsw (embedding vector_cosine_ops);

ANALYZE engine_gate_vectors;

SELECT 'engine_gate_vectors rows: ' || count(*) FROM engine_gate_vectors;
