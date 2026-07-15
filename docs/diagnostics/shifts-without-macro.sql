-- Read-only diagnostic query. Do NOT run automatically against production.
--
-- Lists active shifts that have no calculation macro assigned (shift.macro_id IS NULL), together
-- with how many active, non-scenario work entries are booked against each of them. A high work
-- count next to a missing macro means real payroll data (surcharges, working time) is being
-- calculated without any macro script — the exact symptom of the "default shift macro" bug this
-- package fixes (WorkMacroService.ProcessWorkMacroAsync aborts silently when shift.macro_id is
-- null). Run this after deploying the fix to confirm no new orphans are being created; existing
-- shifts found here predate the fix and were never backfilled (backfilling live data is a
-- deliberate separate decision, not part of this migration).
SELECT
    s.id AS shift_id,
    s.name AS shift_name,
    s.abbreviation AS shift_abbreviation,
    s.status AS shift_status,
    s.create_time AS shift_create_time,
    COUNT(w.id) FILTER (WHERE w.is_deleted = false AND w.analyse_token IS NULL) AS active_work_count
FROM public.shift s
LEFT JOIN public.work w
    ON w.shift_id = s.id
    AND w.is_deleted = false
    AND w.analyse_token IS NULL
WHERE s.is_deleted = false
  AND s.macro_id IS NULL
GROUP BY s.id, s.name, s.abbreviation, s.status, s.create_time
ORDER BY active_work_count DESC, s.create_time DESC;
