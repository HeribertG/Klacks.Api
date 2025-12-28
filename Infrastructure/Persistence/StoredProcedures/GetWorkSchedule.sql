-- GetWorkSchedule: Returns unified view of Work, WorkChange, and Expenses
-- Parameters:
--   start_date: Start of the period
--   end_date: End of the period
--   visible_group_ids: Optional array of visible group IDs for filtering (includes subgroups)
-- Returns: Combined entries from Work, WorkChange, and Expenses with calculated times
-- Entry Types: 0 = Work, 1 = WorkChange, 2 = Expenses

DROP FUNCTION IF EXISTS get_work_schedule(DATE, DATE, UUID[]);

CREATE OR REPLACE FUNCTION get_work_schedule(
    start_date DATE,
    end_date DATE,
    visible_group_ids UUID[] DEFAULT ARRAY[]::UUID[]
)
RETURNS TABLE (
    id UUID,
    entry_type INTEGER,
    work_id UUID,
    client_id UUID,
    entry_date DATE,
    start_shift TIME,
    end_shift TIME,
    change_time NUMERIC,
    work_change_type INTEGER,
    description TEXT,
    amount NUMERIC,
    to_invoice BOOLEAN,
    taxable BOOLEAN,
    shift_id UUID,
    shift_name TEXT,
    abbreviation TEXT,
    replace_client_id UUID,
    is_replacement_entry BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    WITH RECURSIVE group_hierarchy AS (
        SELECT g.id FROM "group" g WHERE g.id = ANY(visible_group_ids) AND array_length(visible_group_ids, 1) > 0
        UNION ALL
        SELECT g.id FROM "group" g
        INNER JOIN group_hierarchy gh ON g.parent = gh.id
    ),
    filtered_shift_ids AS MATERIALIZED (
        SELECT DISTINCT s.id
        FROM shift s
        LEFT JOIN group_item gi ON gi.shift_id = s.id
        WHERE
            (visible_group_ids IS NULL OR array_length(visible_group_ids, 1) IS NULL)
            OR gi.shift_id IS NULL
            OR gi.group_id IN (SELECT ghid.id FROM group_hierarchy ghid)
    ),
    valid_works AS MATERIALIZED (
        SELECT w.*
        FROM work w
        WHERE w.is_deleted = false
        AND w.shift_id IN (SELECT fsi.id FROM filtered_shift_ids fsi)
        AND w."current_date"::DATE >= start_date
        AND w."current_date"::DATE <= end_date
    ),
    -- Entry Type 0: Work entries
    work_entries AS (
        SELECT
            w.id,
            0 AS entry_type,
            w.id AS work_id,
            w.client_id,
            w."current_date"::DATE AS entry_date,
            w.start_shift,
            w.end_shift,
            w.work_time AS change_time,
            NULL::INTEGER AS work_change_type,
            w.information AS description,
            NULL::NUMERIC AS amount,
            NULL::BOOLEAN AS to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry
        FROM valid_works w
        JOIN shift s ON s.id = w.shift_id
    ),
    -- Entry Type 1: WorkChange - CorrectionEnd (Type = 0)
    correction_end_entries AS (
        SELECT
            wc.id,
            1 AS entry_type,
            wc.work_id,
            w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            s.end_shift AS start_shift,
            (s.end_shift + (wc.change_time * INTERVAL '1 minute'))::TIME AS end_shift,
            wc.change_time,
            wc.type AS work_change_type,
            wc.description,
            NULL::NUMERIC AS amount,
            wc.to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            wc.replace_client_id,
            false AS is_replacement_entry
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE wc.is_deleted = false AND wc.type = 0
    ),
    -- Entry Type 1: WorkChange - CorrectionStart (Type = 1)
    correction_start_entries AS (
        SELECT
            wc.id,
            1 AS entry_type,
            wc.work_id,
            w.client_id,
            w."current_date"::DATE AS entry_date,
            (s.start_shift - (wc.change_time * INTERVAL '1 minute'))::TIME AS start_shift,
            s.start_shift AS end_shift,
            wc.change_time,
            wc.type AS work_change_type,
            wc.description,
            NULL::NUMERIC AS amount,
            wc.to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            wc.replace_client_id,
            false AS is_replacement_entry
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE wc.is_deleted = false AND wc.type = 1
    ),
    -- Entry Type 1: WorkChange - ReplacementStart (Type = 2) - Original Client (loses time at start)
    replacement_start_original AS (
        SELECT
            wc.id,
            1 AS entry_type,
            wc.work_id,
            w.client_id,
            w."current_date"::DATE AS entry_date,
            (s.start_shift + (wc.change_time * INTERVAL '1 minute'))::TIME AS start_shift,
            s.end_shift AS end_shift,
            wc.change_time * -1 AS change_time,
            wc.type AS work_change_type,
            wc.description,
            NULL::NUMERIC AS amount,
            wc.to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            wc.replace_client_id,
            false AS is_replacement_entry
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE wc.is_deleted = false AND wc.type = 2
    ),
    -- Entry Type 1: WorkChange - ReplacementStart (Type = 2) - Replacement Client (gains time at start)
    replacement_start_replacement AS (
        SELECT
            wc.id,
            1 AS entry_type,
            wc.work_id,
            wc.replace_client_id AS client_id,
            w."current_date"::DATE AS entry_date,
            s.start_shift AS start_shift,
            (s.start_shift + (wc.change_time * INTERVAL '1 minute'))::TIME AS end_shift,
            wc.change_time,
            wc.type AS work_change_type,
            wc.description,
            NULL::NUMERIC AS amount,
            wc.to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            w.client_id AS replace_client_id,
            true AS is_replacement_entry
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE wc.is_deleted = false AND wc.type = 2 AND wc.replace_client_id IS NOT NULL
    ),
    -- Entry Type 1: WorkChange - ReplacementEnd (Type = 3) - Original Client (loses time at end)
    replacement_end_original AS (
        SELECT
            wc.id,
            1 AS entry_type,
            wc.work_id,
            w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            s.start_shift AS start_shift,
            (s.end_shift - (wc.change_time * INTERVAL '1 minute'))::TIME AS end_shift,
            wc.change_time * -1 AS change_time,
            wc.type AS work_change_type,
            wc.description,
            NULL::NUMERIC AS amount,
            wc.to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            wc.replace_client_id,
            false AS is_replacement_entry
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE wc.is_deleted = false AND wc.type = 3
    ),
    -- Entry Type 1: WorkChange - ReplacementEnd (Type = 3) - Replacement Client (gains time at end)
    replacement_end_replacement AS (
        SELECT
            wc.id,
            1 AS entry_type,
            wc.work_id,
            wc.replace_client_id AS client_id,
            CASE
                WHEN s.end_shift < s.start_shift THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            (s.end_shift - (wc.change_time * INTERVAL '1 minute'))::TIME AS start_shift,
            s.end_shift AS end_shift,
            wc.change_time,
            wc.type AS work_change_type,
            wc.description,
            NULL::NUMERIC AS amount,
            wc.to_invoice,
            NULL::BOOLEAN AS taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            w.client_id AS replace_client_id,
            true AS is_replacement_entry
        FROM work_change wc
        JOIN valid_works w ON w.id = wc.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE wc.is_deleted = false AND wc.type = 3 AND wc.replace_client_id IS NOT NULL
    ),
    -- Entry Type 2: Expenses entries
    expense_entries AS (
        SELECT
            e.id,
            2 AS entry_type,
            e.work_id,
            w.client_id,
            CASE
                WHEN s.end_shift < s.start_shift THEN (w."current_date" + INTERVAL '1 day')::DATE
                ELSE w."current_date"::DATE
            END AS entry_date,
            s.end_shift AS start_shift,
            (s.end_shift + INTERVAL '1 minute')::TIME AS end_shift,
            NULL::NUMERIC AS change_time,
            NULL::INTEGER AS work_change_type,
            e.description,
            e.amount,
            NULL::BOOLEAN AS to_invoice,
            e.taxable,
            w.shift_id,
            s.name AS shift_name,
            s.abbreviation,
            NULL::UUID AS replace_client_id,
            false AS is_replacement_entry
        FROM expenses e
        JOIN valid_works w ON w.id = e.work_id
        JOIN shift s ON s.id = w.shift_id
        WHERE e.is_deleted = false
    )
    -- Combine all entries
    SELECT * FROM work_entries
    UNION ALL
    SELECT * FROM correction_end_entries
    UNION ALL
    SELECT * FROM correction_start_entries
    UNION ALL
    SELECT * FROM replacement_start_original
    UNION ALL
    SELECT * FROM replacement_start_replacement
    UNION ALL
    SELECT * FROM replacement_end_original
    UNION ALL
    SELECT * FROM replacement_end_replacement
    UNION ALL
    SELECT * FROM expense_entries
    ORDER BY client_id, entry_date, start_shift, entry_type;
END;
$$ LANGUAGE plpgsql;
