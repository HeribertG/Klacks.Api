-- Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only
--
-- Engine upgrade gate: state of one database, counted by the gate itself (independent of the updater's own check).
-- Output (psql -At), sorted:
--   rows|<schema>.<table>|<exact row count>     every ordinary and partitioned user table (count(*), no estimate)
--   ext|<name>|<version>                        installed extensions
--   invalid_index|<schema>.<index>              indexes with indisvalid = false (expected: none)
-- Run with psql against each database.

\set ON_ERROR_STOP on
\set QUIET on

SELECT line FROM (
    SELECT 'rows|' || n.nspname || '.' || c.relname || '|' ||
           (xpath('/row/n/text()', query_to_xml(format('SELECT count(*) AS n FROM %I.%I', n.nspname, c.relname), false, true, '')))[1]::text AS line
    FROM pg_class AS c
    JOIN pg_namespace AS n ON n.oid = c.relnamespace
    WHERE c.relkind IN ('r', 'p')
      AND NOT c.relispartition
      AND n.nspname NOT IN ('pg_catalog', 'information_schema')
      AND n.nspname NOT LIKE 'pg\_toast%'
      AND n.nspname NOT LIKE 'pg\_temp%'
    UNION ALL
    SELECT 'ext|' || extname || '|' || extversion FROM pg_extension
    UNION ALL
    SELECT 'invalid_index|' || n.nspname || '.' || c.relname
    FROM pg_index AS i
    JOIN pg_class AS c ON c.oid = i.indexrelid
    JOIN pg_namespace AS n ON n.oid = c.relnamespace
    WHERE NOT i.indisvalid
) AS snapshot
ORDER BY line;
