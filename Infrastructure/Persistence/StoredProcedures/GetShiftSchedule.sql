-- GetShiftSchedule: Generates a shift schedule based on shift weekdays and holidays
-- Parameters:
--   start_date: Start of the period
--   end_date: End of the period
--   holiday_dates: Array of holidays (DATE[])
--   visible_group_ids: Optional array of visible group IDs (includes subgroups)
--   show_ungrouped_shifts: If TRUE, include shifts not assigned to any group (default FALSE)
-- Returns: shift_id, date, day_of_week (ISO: 1=Mon, 7=Sun), shift_name, abbreviation, start_shift, end_shift, work_time, is_sporadic, is_time_range, shift_type, status
-- Note: Only returns shifts with status >= 2 (OriginalShift or SplitShift)

DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[]);
DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[], UUID);
DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[], UUID, UUID[]);
DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[], UUID[]);
DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[], UUID[], BOOLEAN);

CREATE OR REPLACE FUNCTION get_shift_schedule(
    start_date DATE,
    end_date DATE,
    holiday_dates DATE[] DEFAULT ARRAY[]::DATE[],
    visible_group_ids UUID[] DEFAULT ARRAY[]::UUID[],
    show_ungrouped_shifts BOOLEAN DEFAULT FALSE
)
RETURNS TABLE (
    shift_id UUID,
    date DATE,
    day_of_week INTEGER,
    shift_name TEXT,
    abbreviation TEXT,
    start_shift TIME,
    end_shift TIME,
    work_time NUMERIC,
    is_sporadic BOOLEAN,
    is_time_range BOOLEAN,
    shift_type INTEGER,
    status INTEGER,
    is_in_template_container BOOLEAN,
    sum_employees INTEGER,
    quantity INTEGER,
    sporadic_scope INTEGER,
    engaged INTEGER
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
            OR (gi.shift_id IS NULL AND show_ungrouped_shifts)
            OR gi.group_id IN (SELECT id FROM group_hierarchy)
    ),
    holidays AS MATERIALIZED (
        SELECT unnest(holiday_dates) AS holiday_date
    ),
    date_series AS MATERIALIZED (
        SELECT
            d::DATE AS schedule_date,
            EXTRACT(isodow FROM d)::INTEGER AS dow,
            EXISTS (SELECT 1 FROM holidays h WHERE h.holiday_date = d::DATE) AS is_holiday
        FROM generate_series(start_date, end_date, '1 day'::interval) d
    ),
    valid_shifts AS MATERIALIZED (
        SELECT s.*
        FROM shift s
        WHERE s.id IN (SELECT id FROM filtered_shift_ids)
        AND s.is_deleted = false
        AND s.status >= 2  -- Only OriginalShift (2) or SplitShift (3)
        AND s.from_date <= end_date
        AND (s.until_date IS NULL OR s.until_date >= start_date)
    ),
    shift_dates AS (
        SELECT
            s.id AS shift_id,
            d.schedule_date,
            d.dow,
            d.is_holiday,
            s.name AS shift_name,
            s.abbreviation,
            s.start_shift,
            s.end_shift,
            s.work_time,
            s.is_sporadic,
            s.is_time_range,
            s.shift_type,
            s.status,
            s.sum_employees,
            s.quantity,
            s.sporadic_scope
        FROM valid_shifts s
        CROSS JOIN date_series d
        WHERE
            s.from_date <= d.schedule_date
            AND (s.until_date IS NULL OR s.until_date >= d.schedule_date)
            AND (
                (NOT d.is_holiday AND (
                    (d.dow = 1 AND s.is_monday) OR
                    (d.dow = 2 AND s.is_tuesday) OR
                    (d.dow = 3 AND s.is_wednesday) OR
                    (d.dow = 4 AND s.is_thursday) OR
                    (d.dow = 5 AND s.is_friday) OR
                    (d.dow = 6 AND s.is_saturday) OR
                    (d.dow = 7 AND s.is_sunday)
                ))
                OR (s.is_holiday AND d.is_holiday)
                OR (s.is_weekday_and_holiday AND (
                    (d.dow BETWEEN 1 AND 5 AND NOT d.is_holiday) OR d.is_holiday
                ))
            )
    ),
    container_lookup AS MATERIALIZED (
        SELECT DISTINCT
            cti.shift_id,
            d.schedule_date
        FROM container_template_item cti
        JOIN container_template ct ON ct.id = cti.container_template_id
        JOIN shift container ON container.id = ct.container_id
        CROSS JOIN date_series d
        WHERE container.is_deleted = false
        AND container.from_date <= d.schedule_date
        AND (container.until_date IS NULL OR container.until_date >= d.schedule_date)
        AND (
            (ct.weekday = d.dow AND ct.is_holiday = false AND NOT d.is_holiday)
            OR (ct.is_holiday = true AND d.is_holiday)
            OR (ct.is_weekday_and_holiday = true AND (
                (d.dow BETWEEN 1 AND 5 AND NOT d.is_holiday) OR d.is_holiday
            ))
        )
    ),
    work_counts AS MATERIALIZED (
        SELECT
            w.shift_id,
            d.schedule_date,
            COUNT(*)::INTEGER AS engaged_count
        FROM work w
        JOIN date_series d ON d.schedule_date = w."current_date"::DATE
        WHERE w.is_deleted = false
        GROUP BY w.shift_id, d.schedule_date
    )
    SELECT
        sd.shift_id,
        sd.schedule_date AS date,
        sd.dow AS day_of_week,
        sd.shift_name,
        sd.abbreviation,
        sd.start_shift,
        sd.end_shift,
        sd.work_time,
        sd.is_sporadic,
        sd.is_time_range,
        sd.shift_type,
        sd.status,
        (cl.shift_id IS NOT NULL) AS is_in_template_container,
        sd.sum_employees,
        sd.quantity,
        sd.sporadic_scope,
        COALESCE(wc.engaged_count, 0) AS engaged
    FROM shift_dates sd
    LEFT JOIN container_lookup cl ON cl.shift_id = sd.shift_id AND cl.schedule_date = sd.schedule_date
    LEFT JOIN work_counts wc ON wc.shift_id = sd.shift_id AND wc.schedule_date = sd.schedule_date
    ORDER BY sd.schedule_date, sd.shift_name;
END;
$$ LANGUAGE plpgsql;


-- GetShiftSchedulePartial: Returns shift schedule data for specific shift/date pairs
-- Used for partial refresh after Work CRUD operations
-- Parameters:
--   shift_date_pairs: Array of composite type (shift_id UUID, date DATE)
-- Returns: Same columns as get_shift_schedule

DROP TYPE IF EXISTS shift_date_pair CASCADE;
CREATE TYPE shift_date_pair AS (
    shift_id UUID,
    schedule_date DATE
);

DROP FUNCTION IF EXISTS get_shift_schedule_partial(shift_date_pair[]);

CREATE OR REPLACE FUNCTION get_shift_schedule_partial(
    shift_date_pairs shift_date_pair[]
)
RETURNS TABLE (
    shift_id UUID,
    date DATE,
    day_of_week INTEGER,
    shift_name TEXT,
    abbreviation TEXT,
    start_shift TIME,
    end_shift TIME,
    work_time NUMERIC,
    is_sporadic BOOLEAN,
    is_time_range BOOLEAN,
    shift_type INTEGER,
    status INTEGER,
    is_in_template_container BOOLEAN,
    sum_employees INTEGER,
    quantity INTEGER,
    sporadic_scope INTEGER,
    engaged INTEGER
) AS $$
BEGIN
    RETURN QUERY
    WITH input_pairs AS (
        SELECT (unnest(shift_date_pairs)).*
    ),
    shift_data AS (
        SELECT
            s.id AS shift_id,
            ip.schedule_date,
            EXTRACT(isodow FROM ip.schedule_date)::INTEGER AS dow,
            s.name AS shift_name,
            s.abbreviation,
            s.start_shift,
            s.end_shift,
            s.work_time,
            s.is_sporadic,
            s.is_time_range,
            s.shift_type,
            s.status,
            s.sum_employees,
            s.quantity,
            s.sporadic_scope
        FROM input_pairs ip
        JOIN shift s ON s.id = ip.shift_id
        WHERE s.is_deleted = false
        AND s.status >= 2
    ),
    container_lookup AS (
        SELECT DISTINCT
            cti.shift_id,
            ip.schedule_date
        FROM container_template_item cti
        JOIN container_template ct ON ct.id = cti.container_template_id
        JOIN shift container ON container.id = ct.container_id
        JOIN input_pairs ip ON ip.shift_id = cti.shift_id
        WHERE container.is_deleted = false
        AND container.from_date <= ip.schedule_date
        AND (container.until_date IS NULL OR container.until_date >= ip.schedule_date)
    ),
    work_counts AS (
        SELECT
            w.shift_id,
            w."current_date"::DATE AS schedule_date,
            COUNT(*)::INTEGER AS engaged_count
        FROM work w
        JOIN input_pairs ip ON ip.shift_id = w.shift_id AND ip.schedule_date = w."current_date"::DATE
        WHERE w.is_deleted = false
        GROUP BY w.shift_id, w."current_date"::DATE
    )
    SELECT
        sd.shift_id,
        sd.schedule_date AS date,
        sd.dow AS day_of_week,
        sd.shift_name,
        sd.abbreviation,
        sd.start_shift,
        sd.end_shift,
        sd.work_time,
        sd.is_sporadic,
        sd.is_time_range,
        sd.shift_type,
        sd.status,
        (cl.shift_id IS NOT NULL) AS is_in_template_container,
        sd.sum_employees,
        sd.quantity,
        sd.sporadic_scope,
        COALESCE(wc.engaged_count, 0) AS engaged
    FROM shift_data sd
    LEFT JOIN container_lookup cl ON cl.shift_id = sd.shift_id AND cl.schedule_date = sd.schedule_date
    LEFT JOIN work_counts wc ON wc.shift_id = sd.shift_id AND wc.schedule_date = sd.schedule_date
    ORDER BY sd.schedule_date, sd.shift_name;
END;
$$ LANGUAGE plpgsql;
