-- GetShiftSchedule: Generiert einen Dienstplan basierend auf Shift-Wochentagen und Feiertagen
-- Parameter:
--   start_date: Beginn des Zeitraums
--   end_date: Ende des Zeitraums
--   holiday_dates: Array von Feiertagen (DATE[])
-- Rückgabe: ShiftId, Date, DayOfWeek (ISO: 1=Mo, 7=So), ShiftName

CREATE OR REPLACE FUNCTION get_shift_schedule(
    start_date DATE,
    end_date DATE,
    holiday_dates DATE[] DEFAULT ARRAY[]::DATE[]
)
RETURNS TABLE (
    "ShiftId" UUID,
    "Date" DATE,
    "DayOfWeek" INTEGER,
    "ShiftName" TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        s."Id" AS "ShiftId",
        d.schedule_date::DATE AS "Date",
        EXTRACT(isodow FROM d.schedule_date)::INTEGER AS "DayOfWeek",
        s."Name" AS "ShiftName"
    FROM "Shift" s
    CROSS JOIN generate_series(start_date, end_date, '1 day'::interval) d(schedule_date)
    WHERE
        -- Shift muss im Gültigkeitszeitraum liegen
        s."FromDate" <= d.schedule_date::DATE
        AND (s."UntilDate" IS NULL OR s."UntilDate" >= d.schedule_date::DATE)
        -- Shift darf nicht gelöscht sein
        AND s."IsDeleted" = false
        AND (
            -- =====================================================
            -- REGULÄRE WOCHENTAGE (nur wenn KEIN Feiertag)
            -- =====================================================
            (
                d.schedule_date::DATE != ALL(holiday_dates)
                AND s."CuttingAfterMidnight" = false
                AND (
                    (EXTRACT(isodow FROM d.schedule_date) = 1 AND s."IsMonday" = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 2 AND s."IsTuesday" = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 3 AND s."IsWednesday" = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 4 AND s."IsThursday" = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 5 AND s."IsFriday" = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 6 AND s."IsSaturday" = true) OR
                    (EXTRACT(isodow FROM d.schedule_date) = 7 AND s."IsSunday" = true)
                )
            )
            OR
            -- =====================================================
            -- CUTTING AFTER MIDNIGHT (Shift beginnt am Vortag)
            -- Nur wenn KEIN Feiertag
            -- =====================================================
            (
                d.schedule_date::DATE != ALL(holiday_dates)
                AND s."CuttingAfterMidnight" = true
                AND (
                    -- Montag zeigt Shifts die am Sonntag beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 1 AND s."IsSunday" = true) OR
                    -- Dienstag zeigt Shifts die am Montag beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 2 AND s."IsMonday" = true) OR
                    -- Mittwoch zeigt Shifts die am Dienstag beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 3 AND s."IsTuesday" = true) OR
                    -- Donnerstag zeigt Shifts die am Mittwoch beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 4 AND s."IsWednesday" = true) OR
                    -- Freitag zeigt Shifts die am Donnerstag beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 5 AND s."IsThursday" = true) OR
                    -- Samstag zeigt Shifts die am Freitag beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 6 AND s."IsFriday" = true) OR
                    -- Sonntag zeigt Shifts die am Samstag beginnen
                    (EXTRACT(isodow FROM d.schedule_date) = 7 AND s."IsSaturday" = true)
                )
            )
            OR
            -- =====================================================
            -- IS_HOLIDAY: NUR an echten Feiertagen anzeigen
            -- =====================================================
            (
                s."IsHoliday" = true
                AND d.schedule_date::DATE = ANY(holiday_dates)
            )
            OR
            -- =====================================================
            -- IS_WEEKDAY_OR_HOLIDAY: An Wochentagen (Mo-Fr) ODER Feiertagen
            -- =====================================================
            (
                s."IsWeekdayOrHoliday" = true
                AND (
                    -- Wochentage Mo-Fr (und kein Feiertag)
                    (
                        EXTRACT(isodow FROM d.schedule_date) BETWEEN 1 AND 5
                        AND d.schedule_date::DATE != ALL(holiday_dates)
                    )
                    OR
                    -- Oder an Feiertagen
                    d.schedule_date::DATE = ANY(holiday_dates)
                )
            )
        )
    ORDER BY d.schedule_date, s."Name";
END;
$$ LANGUAGE plpgsql;
