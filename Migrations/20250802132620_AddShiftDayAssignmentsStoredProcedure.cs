using Microsoft.EntityFrameworkCore.Migrations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftDayAssignmentsStoredProcedure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
            CREATE OR REPLACE FUNCTION get_shift_day_assignments(
                start_date DATE,
                end_date DATE
            )
            RETURNS TABLE (
                ShiftId UUID,
                Date DATE,
                DayOfWeek INTEGER,
                ShiftName VARCHAR(255)
            ) AS $$
            BEGIN
                RETURN QUERY
                SELECT 
                    s.""Id"" as ShiftId,
                    d.schedule_date::date as Date,
                    EXTRACT(isodow FROM d.schedule_date)::INTEGER as DayOfWeek,
                    s.""Name"" as ShiftName
                FROM ""Shift"" s
                CROSS JOIN generate_series(start_date, end_date, '1 day'::interval) d(schedule_date)
                WHERE 
                    s.""FromDate"" <= d.schedule_date::date AND 
                    (s.""UntilDate"" IS NULL OR s.""UntilDate"" >= d.schedule_date::date)
                    AND (
                        -- Reguläre Wochentage (nur wenn NICHT CuttingAfterMidnight)
                        (EXTRACT(isodow FROM d.schedule_date) = 1 AND s.""IsMonday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 2 AND s.""IsTuesday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 3 AND s.""IsWednesday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 4 AND s.""IsThursday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 5 AND s.""IsFriday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 6 AND s.""IsSaturday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 7 AND s.""IsSunday"" = true AND s.""CuttingAfterMidnight"" = false) OR
                        
                        -- CuttingAfterMidnight: Shift beginnt am vorherigen Tag
                        (EXTRACT(isodow FROM d.schedule_date) = 1 AND s.""CuttingAfterMidnight"" = true AND s.""IsSunday"" = true) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 2 AND s.""CuttingAfterMidnight"" = true AND s.""IsMonday"" = true) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 3 AND s.""CuttingAfterMidnight"" = true AND s.""IsTuesday"" = true) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 4 AND s.""CuttingAfterMidnight"" = true AND s.""IsWednesday"" = true) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 5 AND s.""CuttingAfterMidnight"" = true AND s.""IsThursday"" = true) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 6 AND s.""CuttingAfterMidnight"" = true AND s.""IsFriday"" = true) OR
                        (EXTRACT(isodow FROM d.schedule_date) = 7 AND s.""CuttingAfterMidnight"" = true AND s.""IsSaturday"" = true) OR
                        
                        -- Holiday und WeekdayOrHoliday
                        s.""IsHoliday"" = true OR
                        s.""IsWeekdayOrHoliday"" = true
                    )
                ORDER BY d.schedule_date, s.""Name"";
            END;
            $$ LANGUAGE plpgsql;
        ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS get_shift_day_assignments(DATE, DATE);");
        }
    }
}
