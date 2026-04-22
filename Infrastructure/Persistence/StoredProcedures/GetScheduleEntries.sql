-- GetScheduleEntries: Returns unified view of Work, WorkChange, Expenses, Break, and ScheduleNote
-- Parameters:
--   start_date: Start of the period
--   end_date: End of the period
--   visible_group_ids: Optional array of visible group IDs for filtering (includes subgroups)
-- Returns: Combined entries from Work, WorkChange, Expenses, Break, and ScheduleNote with calculated times
-- Entry Types: 0 = Work, 1 = WorkChange, 2 = Expenses, 3 = Break, 4 = ScheduleNote, 5 = ScheduleCommand
-- Lock Levels: 0 = None, 1 = Confirmed, 2 = Approved, 3 = Closed

DROP FUNCTION IF EXISTS get_work_schedule(DATE, DATE, UUID[]);
DROP FUNCTION IF EXISTS get_work_schedule(DATE, DATE, UUID[], TEXT);
DROP FUNCTION IF EXISTS get_work_schedule(DATE, DATE, UUID[], TEXT, TEXT[]);
DROP FUNCTION IF EXISTS get_schedule_entries(DATE, DATE, UUID[]);
DROP FUNCTION IF EXISTS get_schedule_entries(DATE, DATE, UUID[], UUID);

CREATE OR REPLACE FUNCTION get_schedule_entries(
    start_date DATE,
    end_date DATE,
    visible_group_ids UUID[] DEFAULT ARRAY[]::UUID[],
    p_analyse_token UUID DEFAULT NULL
)
RETURNS TABLE (
    id UUID,
    entry_type INTEGER,
    source_id UUID,
    client_id UUID,
    entry_date DATE,
    start_time TIME,
    end_time TIME,
    change_time NUMERIC,
    surcharges NUMERIC,
    work_change_type INTEGER,
    description TEXT,
    information TEXT,
    amount NUMERIC,
    to_invoice BOOLEAN,
    taxable BOOLEAN,
    entry_id UUID,
    entry_name TEXT,
    abbreviation TEXT,
    replace_client_id UUID,
    is_replacement_entry BOOLEAN,
    lock_level INTEGER,
    is_group_restricted BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    WITH RECURSIVE group_hierarchy AS (
        SELECT g.id FROM "group" g WHERE g.id = ANY(visible_group_ids) AND array_length(visible_group_ids, 1) > 0
        UNION ALL
        SELECT g.id FROM "group" g
        INNER JOIN group_hierarchy gh ON g.parent = gh.id
    ),
    visible_hierarchy_ids AS MATERIALIZED (
        SELECT ghid.id FROM group_hierarchy ghid
    ),
    filtered_shift_ids AS MATERIALIZED (
        SELECT DISTINCT s.id
        FROM shift s
        LEFT JOIN group_item gi ON gi.shift_id = s.id AND gi.is_deleted = false
        WHERE
            ((visible_group_ids IS NULL OR array_length(visible_group_ids, 1) IS NULL)
            OR gi.shift_id IS NULL
            OR gi.group_id IN (SELECT vhi.id FROM visible_hierarchy_ids vhi))
            AND s.analyse_token IS NOT DISTINCT FROM p_analyse_token
    ),
    all_client_shift_ids AS MATERIALIZED (
        SELECT DISTINCT s.id
        FROM shift s
    ),
    valid_works AS MATERIALIZED (
        SELECT w.*
        FROM work w
        WHERE w.is_deleted = false
        AND w.parent_work_id IS NULL
        AND w.shift_id IN (SELECT fsi.id FROM filtered_shift_ids fsi)
        AND w."current_date"::DATE >= start_date
        AND w."current_date"::DATE <= end_date
        AND w.analyse_token IS NOT DISTINCT FROM p_analyse_token
    ),
    work_group_restricted AS MATERIALIZED (
        SELECT
            w.id AS work_id,
            CASE
                WHEN (visible_group_ids IS NOT NULL AND array_length(visible_group_ids, 1) IS NOT NULL)
                    AND EXISTS (SELECT 1 FROM group_item gi2 WHERE gi2.shift_id = w.shift_id AND gi2.is_deleted = false)
                    AND NOT EXISTS (SELECT 1 FROM group_item gi3
                                    WHERE gi3.shift_id = w.shift_id AND gi3.is_deleted = false
                                    AND gi3.group_id IN (SELECT vhi2.id FROM visible_hierarchy_ids vhi2))
                THEN true
                ELSE false
            END AS is_group_restricted
        FROM valid_works w
    ),
    before_shift_offsets AS MATERIALIZED (
        SELECT
            wc.id,
            wc.work_id,
            wc.type,
            wc.change_time,
            CASE wc.type WHEN 1 THEN 0 WHEN 7 THEN 1 WHEN 4 THEN 2 END AS priority,
            COALESCE(SUM(wc.change_time) OVER (
                PARTITION BY wc.work_id
                ORDER BY CASE wc.type WHEN 1 THEN 0 WHEN 7 THEN 1 WHEN 4 THEN 2 END, wc.id
                ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
            ), 0) AS before_offset,
            SUM(wc.change_time) OVER (
                PARTITION BY wc.work_id
                ORDER BY CASE wc.type WHEN 1 THEN 0 WHEN 7 THEN 1 WHEN 4 THEN 2 END, wc.id
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
            ) AS after_offset
        FROM work_change wc
        WHERE wc.is_deleted = false
            AND wc.type IN (1, 7, 4)
            AND wc.work_id IN (SELECT vw.id FROM valid_works vw)
    ),
    after_shift_offsets AS MATERIALIZED (
        SELECT
            wc.id,
            wc.work_id,
            wc.type,
            wc.change_time,
            CASE wc.type WHEN 0 THEN 0 WHEN 8 THEN 1 WHEN 5 THEN 2 END AS priority,
            COALESCE(SUM(wc.change_time) OVER (
                PARTITION BY wc.work_id
                ORDER BY CASE wc.type WHEN 0 THEN 0 WHEN 8 THEN 1 WHEN 5 THEN 2 END, wc.id
                ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING
            ), 0) AS before_offset,
            SUM(wc.change_time) OVER (
                PARTITION BY wc.work_id
                ORDER BY CASE wc.type WHEN 0 THEN 0 WHEN 8 THEN 1 WHEN 5 THEN 2 END, wc.id
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
            ) AS after_offset
        FROM work_change wc
        WHERE wc.is_deleted = false
            AND wc.type IN (0, 8, 5)
            AND wc.work_id IN (SELECT vw.id FROM valid_works vw)
    ),
    -- Entry Type 0: Work entries
    work_entries AS (
        SELECT
            w.id,
            0 AS entry_type,
            w.id AS source_id,
            w.client_id,
            w."current_date"::DATE AS entry_date,
            w.start_time,
            w.end_time,
            w.work_time AS change_time,
            NULL::NUMERIC AS surcharges,
            NULL::INTEGER AS work_change_type,
            NULL::TEXT AS description,
            w.information,
            NULL::NUMERIC AS amount,
            NULL::BOOLEAN AS to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id AS entry_id,
            s.name AS entry_name,
            s.abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry,
            w.lock_level,
            wgr.is_group_restricted
        FROM valid_works w
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
    ),
    -- Entry Type 1: WorkChange - CorrectionEnd (Type = 0) - duration-based, after-shift
    correction_end_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND
                     (w.end_time + aso.before_offset * INTERVAL '1 hour')::TIME < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            (w.end_time + aso.before_offset * INTERVAL '1 hour')::TIME AS start_time,
            (w.end_time + aso.after_offset * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges, wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM after_shift_offsets aso
        JOIN work_change wc ON wc.id = aso.id
        JOIN valid_works w ON w.id = aso.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE aso.type = 0
    ),
    -- Entry Type 1: WorkChange - CorrectionStart (Type = 1) - duration-based, before-shift
    correction_start_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            w."current_date"::DATE AS entry_date,
            (w.start_time - bso.after_offset * INTERVAL '1 hour')::TIME AS start_time,
            (w.start_time - bso.before_offset * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges, wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM before_shift_offsets bso
        JOIN work_change wc ON wc.id = bso.id
        JOIN valid_works w ON w.id = bso.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE bso.type = 1
    ),
    -- Entry Type 1: WorkChange - TravelStart (Type = 4) - duration-based, before-shift
    travel_start_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            w."current_date"::DATE AS entry_date,
            (w.start_time - bso.after_offset * INTERVAL '1 hour')::TIME AS start_time,
            (w.start_time - bso.before_offset * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges, wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM before_shift_offsets bso
        JOIN work_change wc ON wc.id = bso.id
        JOIN valid_works w ON w.id = bso.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE bso.type = 4
    ),
    -- Entry Type 1: WorkChange - TravelEnd (Type = 5) - duration-based, after-shift
    travel_end_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND
                     (w.end_time + aso.before_offset * INTERVAL '1 hour')::TIME < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            (w.end_time + aso.before_offset * INTERVAL '1 hour')::TIME AS start_time,
            (w.end_time + aso.after_offset * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges, wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM after_shift_offsets aso
        JOIN work_change wc ON wc.id = aso.id
        JOIN valid_works w ON w.id = aso.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE aso.type = 5
    ),
    -- Entry Type 1: WorkChange - TravelWithin (Type = 6) - stored Von/Bis
    travel_within_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND wc.start_time < s.end_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            wc.start_time, wc.end_time, wc.change_time, wc.surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 6
    ),
    -- Entry Type 1: WorkChange - Briefing (Type = 7) - duration-based, before-shift
    briefing_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            w."current_date"::DATE AS entry_date,
            (w.start_time - bso.after_offset * INTERVAL '1 hour')::TIME AS start_time,
            (w.start_time - bso.before_offset * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges, wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM before_shift_offsets bso
        JOIN work_change wc ON wc.id = bso.id
        JOIN valid_works w ON w.id = bso.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE bso.type = 7
    ),
    -- Entry Type 1: WorkChange - Debriefing (Type = 8) - duration-based, after-shift
    debriefing_entries AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND
                     (w.end_time + aso.before_offset * INTERVAL '1 hour')::TIME < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            (w.end_time + aso.before_offset * INTERVAL '1 hour')::TIME AS start_time,
            (w.end_time + aso.after_offset * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges, wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM after_shift_offsets aso
        JOIN work_change wc ON wc.id = aso.id
        JOIN valid_works w ON w.id = aso.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE aso.type = 8
    ),
    -- Entry Type 1: WorkChange - ReplacementStart (Type = 2) - duration-based - Original Client (loses time at start)
    replacement_start_original AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            w."current_date"::DATE AS entry_date,
            w.start_time AS start_time,
            (w.start_time + wc.change_time * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time * -1 AS change_time, wc.surcharges * -1 AS surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 2
    ),
    -- Entry Type 1: WorkChange - ReplacementStart (Type = 2) - duration-based - Replacement Client (gains time at start)
    replacement_start_replacement AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, wc.replace_client_id AS client_id,
            w."current_date"::DATE AS entry_date,
            w.start_time AS start_time,
            (w.start_time + wc.change_time * INTERVAL '1 hour')::TIME AS end_time,
            wc.change_time, wc.surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            w.client_id AS replace_client_id, true AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 2 AND wc.replace_client_id IS NOT NULL
    ),
    -- Entry Type 1: WorkChange - ReplacementEnd (Type = 3) - duration-based - Original Client (loses time at end)
    replacement_end_original AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND
                     (w.end_time - wc.change_time * INTERVAL '1 hour')::TIME < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            (w.end_time - wc.change_time * INTERVAL '1 hour')::TIME AS start_time,
            w.end_time AS end_time,
            wc.change_time * -1 AS change_time, wc.surcharges * -1 AS surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 3
    ),
    -- Entry Type 1: WorkChange - ReplacementEnd (Type = 3) - duration-based - Replacement Client (gains time at end)
    replacement_end_replacement AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, wc.replace_client_id AS client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND
                     (w.end_time - wc.change_time * INTERVAL '1 hour')::TIME < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            (w.end_time - wc.change_time * INTERVAL '1 hour')::TIME AS start_time,
            w.end_time AS end_time,
            wc.change_time, wc.surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            w.client_id AS replace_client_id, true AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 3 AND wc.replace_client_id IS NOT NULL
    ),
    -- Entry Type 1: WorkChange - ReplacementWithin (Type = 9) - stored Von/Bis - Original Client (loses time within)
    replacement_within_original AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND wc.start_time < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            wc.start_time, wc.end_time,
            wc.change_time * -1 AS change_time, wc.surcharges * -1 AS surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            wc.replace_client_id, false AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 9
    ),
    -- Entry Type 1: WorkChange - ReplacementWithin (Type = 9) - stored Von/Bis - Replacement Client (gains time within)
    replacement_within_replacement AS (
        SELECT
            wc.id, 1 AS entry_type, wc.work_id AS source_id, wc.replace_client_id AS client_id,
            CASE
                WHEN s.end_shift < s.start_shift AND wc.start_time < s.start_shift
                THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            wc.start_time, wc.end_time, wc.change_time, wc.surcharges,
            wc.type AS work_change_type, wc.description,
            NULL::TEXT AS information, NULL::NUMERIC AS amount, wc.to_invoice,
            NULL::BOOLEAN AS taxable, w.shift_id AS entry_id, s.name AS entry_name, s.abbreviation,
            w.client_id AS replace_client_id, true AS is_replacement_entry, w.lock_level, wgr.is_group_restricted
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE wc.is_deleted = false AND wc.type = 9 AND wc.replace_client_id IS NOT NULL
    ),
    -- Entry Type 2: Expenses entries
    expense_entries AS (
        SELECT
            e.id,
            2 AS entry_type,
            e.work_id AS source_id,
            w.client_id,
            w."current_date"::DATE AS entry_date,
            w.start_time AS start_time,
            (w.start_time + INTERVAL '1 minute')::TIME AS end_time,
            NULL::NUMERIC AS change_time,
            NULL::NUMERIC AS surcharges,
            NULL::INTEGER AS work_change_type,
            e.description,
            NULL::TEXT AS information,
            e.amount,
            NULL::BOOLEAN AS to_invoice,
            e.taxable,
            w.shift_id AS entry_id,
            s.name AS entry_name,
            s.abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry,
            w.lock_level,
            wgr.is_group_restricted
        FROM expenses e
        JOIN valid_works w ON w.id = e.work_id
        JOIN shift s ON s.id = w.shift_id
        JOIN work_group_restricted wgr ON wgr.work_id = w.id
        WHERE e.is_deleted = false
    ),
    -- Entry Type 3: Break entries
    -- Note: entry_name and abbreviation are NULL - frontend looks up absence by entry_id (=absence_id)
    break_entries AS (
        SELECT
            b.id,
            3 AS entry_type,
            b.id AS source_id,
            b.client_id,
            b."current_date"::DATE AS entry_date,
            b.start_time,
            b.end_time,
            b.work_time AS change_time,
            NULL::NUMERIC AS surcharges,
            NULL::INTEGER AS work_change_type,
            b.description::TEXT,
            b.information,
            NULL::NUMERIC AS amount,
            NULL::BOOLEAN AS to_invoice,
            NULL::BOOLEAN AS taxable,
            b.absence_id AS entry_id,
            NULL::TEXT AS entry_name,
            NULL::TEXT AS abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry,
            b.lock_level,
            false AS is_group_restricted
        FROM break b
        WHERE b.is_deleted = false
        AND b.parent_work_id IS NULL
        AND b."current_date"::DATE >= start_date
        AND b."current_date"::DATE <= end_date
        AND b.analyse_token IS NOT DISTINCT FROM p_analyse_token
    ),
    -- Entry Type 4: ScheduleNote entries
    schedule_note_entries AS (
        SELECT
            sn.id,
            4 AS entry_type,
            sn.id AS source_id,
            sn.client_id,
            sn."current_date"::DATE AS entry_date,
            '23:59:59'::TIME AS start_time,
            '23:59:59'::TIME AS end_time,
            NULL::NUMERIC AS change_time,
            NULL::NUMERIC AS surcharges,
            NULL::INTEGER AS work_change_type,
            sn.content AS description,
            NULL::TEXT AS information,
            NULL::NUMERIC AS amount,
            NULL::BOOLEAN AS to_invoice,
            NULL::BOOLEAN AS taxable,
            '00000000-0000-0000-0000-000000000000'::UUID AS entry_id,
            NULL::TEXT AS entry_name,
            NULL::TEXT AS abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry,
            0 AS lock_level,
            false AS is_group_restricted
        FROM schedule_notes sn
        WHERE sn.is_deleted = false
        AND sn."current_date"::DATE >= start_date
        AND sn."current_date"::DATE <= end_date
        AND sn.analyse_token IS NOT DISTINCT FROM p_analyse_token
    ),
    -- Entry Type 5: ScheduleCommand entries
    schedule_command_entries AS (
        SELECT
            sc.id,
            5 AS entry_type,
            sc.id AS source_id,
            sc.client_id,
            sc."current_date"::DATE AS entry_date,
            '23:59:59'::TIME AS start_time,
            '23:59:59'::TIME AS end_time,
            NULL::NUMERIC AS change_time,
            NULL::NUMERIC AS surcharges,
            NULL::INTEGER AS work_change_type,
            sc.command_keyword AS description,
            NULL::TEXT AS information,
            NULL::NUMERIC AS amount,
            NULL::BOOLEAN AS to_invoice,
            NULL::BOOLEAN AS taxable,
            '00000000-0000-0000-0000-000000000000'::UUID AS entry_id,
            NULL::TEXT AS entry_name,
            NULL::TEXT AS abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry,
            0 AS lock_level,
            false AS is_group_restricted
        FROM schedule_commands sc
        WHERE sc.is_deleted = false
        AND sc."current_date"::DATE >= start_date
        AND sc."current_date"::DATE <= end_date
        AND sc.analyse_token IS NOT DISTINCT FROM p_analyse_token
    )
    -- Combine all entries
    SELECT * FROM (
        SELECT * FROM work_entries
        UNION ALL SELECT * FROM correction_end_entries
        UNION ALL SELECT * FROM correction_start_entries
        UNION ALL SELECT * FROM travel_start_entries
        UNION ALL SELECT * FROM travel_end_entries
        UNION ALL SELECT * FROM travel_within_entries
        UNION ALL SELECT * FROM briefing_entries
        UNION ALL SELECT * FROM debriefing_entries
        UNION ALL SELECT * FROM replacement_start_original
        UNION ALL SELECT * FROM replacement_start_replacement
        UNION ALL SELECT * FROM replacement_end_original
        UNION ALL SELECT * FROM replacement_end_replacement
        UNION ALL SELECT * FROM replacement_within_original
        UNION ALL SELECT * FROM replacement_within_replacement
        UNION ALL SELECT * FROM expense_entries
        UNION ALL SELECT * FROM break_entries
        UNION ALL SELECT * FROM schedule_note_entries
        UNION ALL SELECT * FROM schedule_command_entries
    ) AS combined
    ORDER BY combined.client_id, combined.entry_date, combined.start_time,
        CASE combined.entry_type
            WHEN 0 THEN 0  -- Work first
            WHEN 2 THEN 1  -- Expenses second
            WHEN 1 THEN 2  -- WorkChange third
            WHEN 3 THEN 3  -- Break fourth
            WHEN 4 THEN 4  -- ScheduleNote fifth
            WHEN 5 THEN 5  -- ScheduleCommand last
        END;
END;
$$ LANGUAGE plpgsql;
