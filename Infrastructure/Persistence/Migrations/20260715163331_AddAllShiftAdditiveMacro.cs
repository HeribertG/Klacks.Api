using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAllShiftAdditiveMacro : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seeds the second structural standard macro (K4): "AllShiftAdditive" stacks night, weekend
            // and holiday surcharges on top of each other (KR/VN/PL semantics) instead of the seeded
            // "AllShift" highest-wins cascade. Fixed row id, category Shift (1), function
            // StandardAdditive (2). Idempotent and deferential: skipped when the row already exists or
            // when the installation already carries its own active StandardAdditive macro for category
            // Shift (full-CRUD guarantee — a customer's own macro is never displaced, and the partial
            // unique index ix_macro_category_type would reject a second (1, 2) row anyway).
            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id, ""name"", ""content"", ""type"", description, create_time, current_user_created, update_time, current_user_updated, deleted_time, is_deleted, current_user_deleted, category)
SELECT 'e4a71d2c-5b8f-4c3a-9d16-84f0b2a7c9e3', 'AllShiftAdditive', 'IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, WE1Rate, WE2Rate, WE3Rate
IMPORT NightStart, NightEnd
IMPORT WeekendDay1, WeekendDay2, WeekendDay3

FUNCTION SegBonusForType(StartTime, EndTime, HolidayFlag, WeekdayNum, WantType)
    DIM SegmentHours, NightHours, Amount
    DIM HasHoliday, IsWE1, IsWE2, IsWE3

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = TimeOverlap(NightStart, NightEnd, StartTime, EndTime)

    HasHoliday = HolidayFlag = 1
    IsWE1 = WeekdayNum = WeekendDay1
    IsWE2 = WeekdayNum = WeekendDay2
    IsWE3 = WeekdayNum = WeekendDay3

    Amount = 0
    IF WantType = 10 THEN
        Amount = NightHours * NightRate
    ENDIF
    IF WantType = 11 THEN
        IF IsWE1 THEN Amount = SegmentHours * WE1Rate ENDIF
    ENDIF
    IF WantType = 12 THEN
        IF IsWE2 THEN Amount = SegmentHours * WE2Rate ENDIF
    ENDIF
    IF WantType = 13 THEN
        IF IsWE3 THEN Amount = SegmentHours * WE3Rate ENDIF
    ENDIF
    IF WantType = 14 THEN
        IF HasHoliday THEN Amount = SegmentHours * HolidayRate ENDIF
    ENDIF

    SegBonusForType = Amount
ENDFUNCTION

DIM TotalBonus, WeekdayNextDay
DIM BonusNight, BonusWeekend1, BonusWeekend2, BonusWeekend3, BonusHoliday

WeekdayNextDay = (Weekday MOD 7) + 1

IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
    BonusNight = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 10) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 10)
    BonusWeekend1 = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 11) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 11)
    BonusWeekend2 = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 12) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 12)
    BonusWeekend3 = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 13) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 13)
    BonusHoliday = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 14) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 14)
ELSE
    BonusNight = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 10)
    BonusWeekend1 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 11)
    BonusWeekend2 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 12)
    BonusWeekend3 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 13)
    BonusHoliday = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 14)
ENDIF

TotalBonus = BonusNight + BonusWeekend1 + BonusWeekend2 + BonusWeekend3 + BonusHoliday

OUTPUT 1, Round(TotalBonus, 2)
OUTPUT 10, BonusNight
OUTPUT 11, BonusWeekend1
OUTPUT 12, BonusWeekend2
OUTPUT 13, BonusWeekend3
OUTPUT 14, BonusHoliday
', 2, '{""de"":""""}', '2026-07-15 12:00:00.000', 'admin', NULL, '', NULL, false, '', 1
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = 'e4a71d2c-5b8f-4c3a-9d16-84f0b2a7c9e3')
  AND NOT EXISTS (SELECT 1 FROM public.macro WHERE category = 1 AND type = 2 AND is_deleted = false);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM public.macro WHERE id = 'e4a71d2c-5b8f-4c3a-9d16-84f0b2a7c9e3';");
        }
    }
}
