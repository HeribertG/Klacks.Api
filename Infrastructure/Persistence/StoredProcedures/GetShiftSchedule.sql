-- GetShiftSchedule: Generates a shift schedule based on shift weekdays and holidays
-- Parameters:
--   start_date: Start of the period
--   end_date: End of the period
--   holiday_dates: Array of holidays (DATE[])
--   selected_group_id: Optional group filter (includes subgroups)
-- Returns: shift_id, date, day_of_week (ISO: 1=Mon, 7=Sun), shift_name, abbreviation, start_shift, end_shift, work_time, is_sporadic, is_time_range

DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[]);
DROP FUNCTION IF EXISTS get_shift_schedule(DATE, DATE, DATE[], UUID);

CREATE OR REPLACE FUNCTION get_shift_schedule(
    start_date DATE,
    end_date DATE,
    holiday_dates DATE[] DEFAULT ARRAY[]::DATE[],
    selected_group_id UUID DEFAULT NULL
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
    shift_type INTEGER
) AS $$
BEGIN
    RETURN QUERY
    WITH RECURSIVE group_hierarchy AS (
        -- Base case: selected group (if provided)
        SELECT g.id FROM "group" g WHERE g.id = selected_group_id
        UNION ALL
        -- Recursive case: all child groups
        SELECT g.id FROM "group" g
        INNER JOIN group_hierarchy gh ON g.parent = gh.id
    ),
    filtered_shift_ids AS (
        -- If no group selected, return all shifts
        -- If group selected, return shifts with no groups OR shifts in the group hierarchy
        SELECT DISTINCT s.id
        FROM shift s
        LEFT JOIN group_item gi ON gi.shift_id = s.id
        WHERE selected_group_id IS NULL
           OR gi.shift_id IS NULL  -- Shift has no groups
           OR gi.group_id IN (SELECT id FROM group_hierarchy)
    )
    SELECT
        s.id AS shift_id,
        d.schedule_date::DATE AS date,
        EXTRACT(isodow FROM d.schedule_date)::INTEGER AS day_of_week,
        s.name AS shift_name,
        s.abbreviation AS abbreviation,
        s.start_shift AS start_shift,
        s.end_shift AS end_shift,
        s.work_time AS work_time,
        s.is_sporadic AS is_sporadic,
        s.is_time_range AS is_time_range,
        s.shift_type AS shift_type
    FROM shift s
    CROSS JOIN generate_series(start_date, end_date, '1 day'::interval) d(schedule_date)
    WHERE
        -- Group filter
        s.id IN (SELECT id FROM filtered_shift_ids)
        AND
        -- Shift must be within validity period
        s.from_date <= d.schedule_date::DATE
        AND (s.until_date IS NULL OR s.until_date >= d.schedule_date::DATE)
        -- Shift must not be deleted
        AND s.is_deleted = false
        AND (
            -- =====================================================
            -- REGULAR WEEKDAYS (only when NOT a holiday)
            -- =====================================================
            (
                d.schedule_date::DATE != ALL(holiday_dates)
                AND s.cutting_after_midnight = false
                AND (
                    (EXTRACT(isodow FROM d.schedule_date) = 1 AND s.is_monday = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 2 AND s.is_tuesday = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 3 AND s.is_wednesday = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 4 AND s.is_thursday = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 5 AND s.is_friday = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 6 AND s.is_saturday = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 7 AND s.is_sunday = true)
                )
            )
            OR
            -- =====================================================
            -- CUTTING AFTER MIDNIGHT (shift starts on previous day)
            -- Only when NOT a holiday
            -- =====================================================
            (
                d.schedule_date::DATE != ALL(holiday_dates)
                AND s.cutting_after_midnight = true
                AND (
                    -- Monday shows shifts that start on Sunday
                    (EXTRACT(isodow FROM d.schedule_date) = 1 AND s.is_sunday = true) OR
                    -- Tuesday shows shifts that start on Monday
                    (EXTRACT(isodow FROM d.schedule_date) = 2 AND s.is_monday = true) OR
                    -- Wednesday shows shifts that start on Tuesday
                    (EXTRACT(isodow FROM d.schedule_date) = 3 AND s.is_tuesday = true) OR
                    -- Thursday shows shifts that start on Wednesday
                    (EXTRACT(isodow FROM d.schedule_date) = 4 AND s.is_wednesday = true) OR
                    -- Friday shows shifts that start on Thursday
                    (EXTRACT(isodow FROM d.schedule_date) = 5 AND s.is_thursday = true) OR
                    -- Saturday shows shifts that start on Friday
                    (EXTRACT(isodow FROM d.schedule_date) = 6 AND s.is_friday = true) OR
                    -- Sunday shows shifts that start on Saturday
                    (EXTRACT(isodow FROM d.schedule_date) = 7 AND s.is_saturday = true)
                )
            )
            OR
            -- =====================================================
            -- IS_HOLIDAY: Show ONLY on actual holidays
            -- =====================================================
            (
                s.is_holiday = true
                AND d.schedule_date::DATE = ANY(holiday_dates)
            )
            OR
            -- =====================================================
            -- IS_WEEKDAY_AND_HOLIDAY: On weekdays (Mon-Fri) AND on holidays
            -- =====================================================
            (
                s.is_weekday_and_holiday = true
                AND (
                    -- Weekdays Mon-Fri (and not a holiday)
                    (
                        EXTRACT(isodow FROM d.schedule_date) BETWEEN 1 AND 5
                        AND d.schedule_date::DATE != ALL(holiday_dates)
                    )
                    OR
                    -- Or on holidays
                    d.schedule_date::DATE = ANY(holiday_dates)
                )
            )
        )
    ORDER BY d.schedule_date, s.name;
END;
$$ LANGUAGE plpgsql;
