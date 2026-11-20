-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: proves that the HNSW index engine_gate_vectors_embedding_hnsw is usable after the restore.
-- For each of the 20 probes: with enable_seqscan = off the plan (EXPLAIN FORMAT JSON) must be an index scan on that
-- index, and recall@10 of the index result against the exact result (index scans off) must average >= 0.9. The plan
-- the planner picks with default settings is only logged. Every query runs through EXECUTE, so it is planned anew
-- with the settings of that moment. Fails (psql exit code 3) on any violation. Run with psql against Klacks.

\set ON_ERROR_STOP on
\set QUIET on
\ir gate-probe-vectors.sql

SET hnsw.ef_search = 100;

DO $$
DECLARE
    index_name constant text := 'engine_gate_vectors_embedding_hnsw';
    minimum_recall constant numeric := 0.9;
    neighbour_count constant integer := 10;
    probe record;
    plan_text text;
    default_plan text;
    approximate integer[];
    exact integer[];
    hits integer := 0;
    total integer := 0;
    recall numeric;
    query_text text;
BEGIN
    FOR probe IN SELECT p, embedding::text AS literal FROM gate_probes ORDER BY p LOOP
        query_text := format('SELECT id FROM engine_gate_vectors ORDER BY embedding <=> %L::vector LIMIT %s', probe.literal, neighbour_count);

        PERFORM set_config('enable_seqscan', 'on', true);
        PERFORM set_config('enable_indexscan', 'on', true);
        EXECUTE 'EXPLAIN (FORMAT JSON) ' || query_text INTO default_plan;
        IF probe.p = 1 THEN
            RAISE NOTICE 'default plan uses index: %', position(format('"Index Name": "%s"', index_name) IN default_plan) > 0;
        END IF;

        PERFORM set_config('enable_seqscan', 'off', true);
        EXECUTE 'EXPLAIN (FORMAT JSON) ' || query_text INTO plan_text;
        IF position('"Node Type": "Index Scan"' IN plan_text) = 0
           OR position(format('"Index Name": "%s"', index_name) IN plan_text) = 0 THEN
            RAISE EXCEPTION 'probe %: no index scan on % with enable_seqscan = off: %', probe.p, index_name, plan_text;
        END IF;
        EXECUTE format('SELECT array_agg(id) FROM (%s) AS q', query_text) INTO approximate;

        PERFORM set_config('enable_seqscan', 'on', true);
        PERFORM set_config('enable_indexscan', 'off', true);
        EXECUTE format('SELECT array_agg(id) FROM (SELECT id FROM engine_gate_vectors ORDER BY embedding <=> %L::vector, id LIMIT %s) AS q',
                       probe.literal, neighbour_count) INTO exact;

        hits := hits + (SELECT count(*) FROM unnest(approximate) AS a(id) WHERE a.id = ANY (exact));
        total := total + neighbour_count;
    END LOOP;

    recall := round(hits::numeric / total, 4);
    RAISE NOTICE 'HNSW recall@10 over 20 probes: % (ef_search %)', recall, current_setting('hnsw.ef_search');
    IF recall < minimum_recall THEN
        RAISE EXCEPTION 'HNSW recall@10 % is below %', recall, minimum_recall;
    END IF;
END
$$;

SELECT 'hnsw|ok';
