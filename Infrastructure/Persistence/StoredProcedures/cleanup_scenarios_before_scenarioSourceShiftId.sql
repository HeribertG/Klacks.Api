-- Idempotent cleanup of all AnalyseScenario rows and their clones that
-- existed before the ScenarioSourceShiftId mechanism was introduced.
-- Pre-migration clones have no scenario_source_shift_id and would
-- short-circuit ValidateNoAcceptConflictsAsync (no checks fire) while
-- PromoteScenarioWorksAsync would skip the shift-id remap, leaving
-- clone-shift references in promoted works after accept.
-- Run once after migrations 20260510092137 + 20260510092438 are applied.

DO $$
DECLARE
    cleanup_user CONSTANT TEXT := 'cleanup-pre-scenarioSource-2026-05-10';
BEGIN
    WITH scenario_tokens AS (
        SELECT token FROM analyse_scenarios WHERE NOT is_deleted
    ),
    cloned_work_ids AS (
        SELECT id FROM work
        WHERE NOT is_deleted
          AND analyse_token IN (SELECT token FROM scenario_tokens)
    )
    UPDATE work_change SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND work_id IN (SELECT id FROM cloned_work_ids);

    WITH scenario_tokens AS (
        SELECT token FROM analyse_scenarios WHERE NOT is_deleted
    ),
    cloned_work_ids AS (
        SELECT id FROM work
        WHERE NOT is_deleted
          AND analyse_token IN (SELECT token FROM scenario_tokens)
    )
    UPDATE expenses SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND work_id IN (SELECT id FROM cloned_work_ids);

    UPDATE work SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND analyse_token IN (SELECT token FROM analyse_scenarios WHERE NOT is_deleted);

    UPDATE break SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND analyse_token IN (SELECT token FROM analyse_scenarios WHERE NOT is_deleted);

    UPDATE schedule_notes SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND analyse_token IN (SELECT token FROM analyse_scenarios WHERE NOT is_deleted);

    UPDATE work_softening SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND analyse_token IN (SELECT token FROM analyse_scenarios WHERE NOT is_deleted);

    UPDATE shift SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted
      AND analyse_token IN (SELECT token FROM analyse_scenarios WHERE NOT is_deleted);

    UPDATE analyse_scenarios SET
        is_deleted = TRUE,
        deleted_time = NOW(),
        current_user_deleted = cleanup_user
    WHERE NOT is_deleted;
END $$;

SELECT
    (SELECT COUNT(*) FROM analyse_scenarios WHERE NOT is_deleted) AS scenarios_remaining,
    (SELECT COUNT(*) FROM shift WHERE analyse_token IS NOT NULL AND NOT is_deleted) AS clone_shifts_remaining,
    (SELECT COUNT(*) FROM work WHERE analyse_token IS NOT NULL AND NOT is_deleted) AS clone_works_remaining,
    (SELECT COUNT(*) FROM break WHERE analyse_token IS NOT NULL AND NOT is_deleted) AS clone_breaks_remaining,
    (SELECT COUNT(*) FROM schedule_notes WHERE analyse_token IS NOT NULL AND NOT is_deleted) AS clone_notes_remaining;
