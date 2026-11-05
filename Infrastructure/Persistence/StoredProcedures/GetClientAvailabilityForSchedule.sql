-- GetClientAvailabilityForSchedule: Returns per client and day the available and the blocked hours as ranges
-- Parameters:
--   p_start_date: First day of the period
--   p_end_date: Last day of the period
--   p_client_ids: Clients to read
-- Returns: one row per client and day that has at least one non-deleted availability row
--   availability_ranges: hours marked available, "HH:00-HH:00" comma-separated, end hour exclusive; '' when none
--   blocked_ranges: hours that cannot be planned, same format ("24:00" as end allowed); '' when none.
--     Mirrors AvailabilityMatcher: a day with at least one available hour blocks every hour that is not marked
--     available; a day with only unavailable rows blocks exactly those hours. Days without rows return no row.

DROP FUNCTION IF EXISTS get_client_availability_for_schedule(DATE, DATE, UUID[]);

CREATE OR REPLACE FUNCTION get_client_availability_for_schedule(
    p_start_date DATE,
    p_end_date DATE,
    p_client_ids UUID[]
)
RETURNS TABLE (
    client_id UUID,
    availability_date DATE,
    availability_ranges TEXT,
    blocked_ranges TEXT
) AS $$
BEGIN
    RETURN QUERY
    WITH marked AS (
        SELECT ca.client_id, ca.date, ca.hour, ca.is_available
        FROM client_availability ca
        WHERE ca.is_deleted = false
        AND ca.date >= p_start_date
        AND ca.date <= p_end_date
        AND ca.client_id = ANY(p_client_ids)
    ),
    marked_days AS (
        SELECT m.client_id, m.date, BOOL_OR(m.is_available) AS has_available
        FROM marked m
        GROUP BY m.client_id, m.date
    ),
    hour_states AS (
        SELECT
            md.client_id,
            md.date,
            gs.hour,
            CASE
                WHEN m.is_available IS TRUE THEN TRUE
                WHEN md.has_available OR m.is_available IS FALSE THEN FALSE
            END AS is_plannable
        FROM marked_days md
        CROSS JOIN generate_series(0, 23) AS gs(hour)
        LEFT JOIN marked m
            ON m.client_id = md.client_id
            AND m.date = md.date
            AND m.hour = gs.hour
    ),
    hour_groups AS (
        SELECT
            hs.client_id,
            hs.date,
            hs.is_plannable,
            hs.hour,
            hs.hour - ROW_NUMBER() OVER (
                PARTITION BY hs.client_id, hs.date, hs.is_plannable ORDER BY hs.hour)::INT AS grp
        FROM hour_states hs
        WHERE hs.is_plannable IS NOT NULL
    ),
    ranges AS (
        SELECT
            hg.client_id,
            hg.date,
            hg.is_plannable,
            MIN(hg.hour) AS start_hour,
            MAX(hg.hour) + 1 AS end_hour
        FROM hour_groups hg
        GROUP BY hg.client_id, hg.date, hg.is_plannable, hg.grp
    ),
    aggregated AS (
        SELECT
            r.client_id,
            r.date,
            STRING_AGG(
                LPAD(r.start_hour::TEXT, 2, '0') || ':00-' || LPAD(r.end_hour::TEXT, 2, '0') || ':00',
                ',' ORDER BY r.start_hour
            ) FILTER (WHERE r.is_plannable) AS available_text,
            STRING_AGG(
                LPAD(r.start_hour::TEXT, 2, '0') || ':00-' || LPAD(r.end_hour::TEXT, 2, '0') || ':00',
                ',' ORDER BY r.start_hour
            ) FILTER (WHERE NOT r.is_plannable) AS blocked_text
        FROM ranges r
        GROUP BY r.client_id, r.date
    )
    SELECT
        md.client_id,
        md.date AS availability_date,
        COALESCE(a.available_text, '') AS availability_ranges,
        COALESCE(a.blocked_text, '') AS blocked_ranges
    FROM marked_days md
    LEFT JOIN aggregated a
        ON a.client_id = md.client_id
        AND a.date = md.date
    ORDER BY md.client_id, md.date;
END;
$$ LANGUAGE plpgsql;
