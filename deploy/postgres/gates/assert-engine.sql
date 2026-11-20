-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: engine facts of the Klacks database. Fails (psql exit code 3) unless
--   * the server major (server_version_num / 10000) equals :expected_major,
--   * extension vector is installed, in version :expected_vector unless that variable is empty,
--   * extension pg_trgm is installed,
--   * knowledge_index_embedding_idx and engine_gate_vectors_embedding_hnsw exist, are valid and use hnsw,
--   * no index of the database is invalid.
-- Run with psql against Klacks: -v expected_major=19 -v expected_vector=0.8.7 (expected_vector= skips the version).

\set ON_ERROR_STOP on
\set QUIET on

SELECT set_config('klacks_gate.expected_major', :'expected_major', false) AS expected_major_set \gset
SELECT set_config('klacks_gate.expected_vector', :'expected_vector', false) AS expected_vector_set \gset

DO $$
DECLARE
    expected_major constant integer := current_setting('klacks_gate.expected_major')::integer;
    expected_vector constant text := current_setting('klacks_gate.expected_vector');
    server_major integer := current_setting('server_version_num')::integer / 10000;
    vector_version text;
    required_index text;
    invalid_count integer;
BEGIN
    IF server_major <> expected_major THEN
        RAISE EXCEPTION 'server major is %, expected %', server_major, expected_major;
    END IF;

    SELECT extversion INTO vector_version FROM pg_extension WHERE extname = 'vector';
    IF vector_version IS NULL THEN
        RAISE EXCEPTION 'extension vector is not installed';
    END IF;
    IF expected_vector <> '' AND vector_version <> expected_vector THEN
        RAISE EXCEPTION 'extension vector is %, expected %', vector_version, expected_vector;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'pg_trgm') THEN
        RAISE EXCEPTION 'extension pg_trgm is not installed';
    END IF;

    FOREACH required_index IN ARRAY ARRAY['knowledge_index_embedding_idx', 'engine_gate_vectors_embedding_hnsw'] LOOP
        IF NOT EXISTS (
            SELECT 1
            FROM pg_index AS i
            JOIN pg_class AS c ON c.oid = i.indexrelid
            JOIN pg_am AS am ON am.oid = c.relam
            WHERE c.relname = required_index AND i.indisvalid AND i.indisready AND am.amname = 'hnsw'
        ) THEN
            RAISE EXCEPTION 'index % is missing, invalid or not hnsw', required_index;
        END IF;
    END LOOP;

    SELECT count(*) INTO invalid_count FROM pg_index WHERE NOT indisvalid;
    IF invalid_count > 0 THEN
        RAISE EXCEPTION '% invalid index(es)', invalid_count;
    END IF;

    RAISE NOTICE 'engine ok: PostgreSQL % (%), vector %', server_major, current_setting('server_version'), vector_version;
END
$$;

SELECT 'engine|' || current_setting('server_version_num') || '|' || (SELECT extversion FROM pg_extension WHERE extname = 'vector');
