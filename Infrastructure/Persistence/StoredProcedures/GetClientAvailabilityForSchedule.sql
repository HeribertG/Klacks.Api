CREATE OR REPLACE FUNCTION get_client_availability_for_schedule(
    p_start_date DATE,
    p_end_date DATE,
    p_client_ids UUID[]
)
RETURNS TABLE (
    client_id UUID,
    availability_date DATE,
    availability_ranges TEXT
) AS $$
BEGIN
    RETURN QUERY
    WITH available_hours AS (
        SELECT ca.client_id, ca.date, ca.hour
        FROM client_availability ca
        WHERE ca.is_deleted = false
        AND ca.is_available = true
        AND ca.date >= p_start_date
        AND ca.date <= p_end_date
        AND ca.client_id = ANY(p_client_ids)
    ),
    hour_groups AS (
        SELECT
            ah.client_id,
            ah.date,
            ah.hour,
            ah.hour - ROW_NUMBER() OVER (PARTITION BY ah.client_id, ah.date ORDER BY ah.hour)::INT AS grp
        FROM available_hours ah
    ),
    ranges AS (
        SELECT
            hg.client_id,
            hg.date,
            MIN(hg.hour) AS start_hour,
            MAX(hg.hour) + 1 AS end_hour
        FROM hour_groups hg
        GROUP BY hg.client_id, hg.date, hg.grp
    )
    SELECT
        r.client_id,
        r.date AS availability_date,
        STRING_AGG(
            LPAD(r.start_hour::TEXT, 2, '0') || ':00-' || LPAD(r.end_hour::TEXT, 2, '0') || ':00',
            ',' ORDER BY r.start_hour
        ) AS availability_ranges
    FROM ranges r
    GROUP BY r.client_id, r.date
    ORDER BY r.client_id, r.date;
END;
$$ LANGUAGE plpgsql;
