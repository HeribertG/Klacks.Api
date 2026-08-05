-- Copyright (c) Heribert Gasparoli Private. All rights reserved.
--
-- Repairs WizardRunCapture rows that the seal sweep stamped as Accepted unconditionally
-- (behaviour before the MeasureResolvedAsync status gate, captures written since 2026-07-16).
-- A capture whose scenario was deleted or rejected becomes Rejected; a capture whose scenario
-- was never promoted (still Active) becomes Superseded. The churn values are cleared because
-- they were measured against a proposal that was never realised.
--
-- MANUAL SCRIPT. Deliberately NOT under Infrastructure/Persistence/StoredProcedures/ - every
-- .sql file there is an EmbeddedResource that StoredProcedureInitializer executes on each
-- backend start. Run this only after an explicit owner decision, and run the SELECT dry-runs
-- below first. Idempotent: the outcome = 0 guard makes repeated runs a no-op.
--
-- Enum values: CaptureOutcome Accepted=0, Rejected=1, Superseded=2, Expired=3
--              AnalyseScenarioStatus Active=0, Accepted=1, Rejected=2

-- === DRY RUN 1: captures of deleted or rejected scenarios ===
-- SELECT count(*) FROM wizard_run_capture c
-- JOIN analyse_scenarios s ON c.scenario_id = s.id
-- WHERE c.outcome = 0 AND (s.is_deleted = true OR s.status = 2);

-- === DRY RUN 2: captures of scenarios that were never promoted ===
-- SELECT count(*) FROM wizard_run_capture c
-- JOIN analyse_scenarios s ON c.scenario_id = s.id
-- WHERE c.outcome = 0 AND s.is_deleted = false AND s.status = 0;

UPDATE wizard_run_capture c
SET outcome = 1,
    correction_churn = NULL,
    event_churn = NULL,
    measured_at = NULL
FROM analyse_scenarios s
WHERE c.scenario_id = s.id
  AND c.outcome = 0
  AND (s.is_deleted = true OR s.status = 2);

UPDATE wizard_run_capture c
SET outcome = 2,
    correction_churn = NULL,
    event_churn = NULL,
    measured_at = NULL
FROM analyse_scenarios s
WHERE c.scenario_id = s.id
  AND c.outcome = 0
  AND s.is_deleted = false
  AND s.status = 0;
