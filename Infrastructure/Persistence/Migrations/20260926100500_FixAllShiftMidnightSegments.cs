using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixAllShiftMidnightSegments : Migration
    {
        private const string AllShiftName = "AllShift";
        private const string AllShiftAdditiveName = "AllShiftAdditive";
        private const string CarriageReturn = "\r";
        private const string LineBreak = "\n";
        private const string CrLf = "\r\n";

        private const string OldAllShift = @"IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, WE1Rate, WE2Rate, WE3Rate
IMPORT NightStart, NightEnd
IMPORT WeekendDay1, WeekendDay2, WeekendDay3

FUNCTION SegBonusForType(StartTime, EndTime, HolidayFlag, WeekdayNum, WantType)
    DIM SegmentHours, NightHours, NonNightHours, Amount
    DIM NRate, DRate, NType, DType
    DIM HasHoliday, IsWE1, IsWE2, IsWE3

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = TimeOverlap(NightStart, NightEnd, StartTime, EndTime)
    NonNightHours = SegmentHours - NightHours

    HasHoliday = HolidayFlag = 1
    IsWE1 = WeekdayNum = WeekendDay1
    IsWE2 = WeekdayNum = WeekendDay2
    IsWE3 = WeekdayNum = WeekendDay3

    NRate = 0
    NType = 0
    IF NightHours > 0 THEN
        NRate = NightRate
        NType = 10
    ENDIF
    IF HasHoliday AndAlso HolidayRate > NRate THEN
        NRate = HolidayRate
        NType = 14
    ENDIF
    IF IsWE1 AndAlso WE1Rate > NRate THEN
        NRate = WE1Rate
        NType = 11
    ENDIF
    IF IsWE2 AndAlso WE2Rate > NRate THEN
        NRate = WE2Rate
        NType = 12
    ENDIF
    IF IsWE3 AndAlso WE3Rate > NRate THEN
        NRate = WE3Rate
        NType = 13
    ENDIF

    DRate = 0
    DType = 0
    IF HasHoliday AndAlso HolidayRate > DRate THEN
        DRate = HolidayRate
        DType = 14
    ENDIF
    IF IsWE1 AndAlso WE1Rate > DRate THEN
        DRate = WE1Rate
        DType = 11
    ENDIF
    IF IsWE2 AndAlso WE2Rate > DRate THEN
        DRate = WE2Rate
        DType = 12
    ENDIF
    IF IsWE3 AndAlso WE3Rate > DRate THEN
        DRate = WE3Rate
        DType = 13
    ENDIF

    Amount = 0
    IF NType = WantType THEN Amount = Amount + NightHours * NRate ENDIF
    IF DType = WantType THEN Amount = Amount + NonNightHours * DRate ENDIF

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
";

        private const string NewAllShift = @"IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, WE1Rate, WE2Rate, WE3Rate
IMPORT NightStart, NightEnd
IMPORT WeekendDay1, WeekendDay2, WeekendDay3

FUNCTION SegBonusForType(StartTime, EndTime, HolidayFlag, WeekdayNum, WantType)
    DIM SegmentHours, NightHours, NonNightHours, Amount
    DIM NRate, DRate, NType, DType
    DIM HasHoliday, IsWE1, IsWE2, IsWE3

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = 0
    IF SegmentHours > 0 THEN NightHours = TimeOverlap(NightStart, NightEnd, StartTime, EndTime) ENDIF
    NonNightHours = SegmentHours - NightHours

    HasHoliday = HolidayFlag = 1
    IsWE1 = WeekdayNum = WeekendDay1
    IsWE2 = WeekdayNum = WeekendDay2
    IsWE3 = WeekdayNum = WeekendDay3

    NRate = 0
    NType = 0
    IF NightHours > 0 THEN
        NRate = NightRate
        NType = 10
    ENDIF
    IF HasHoliday AndAlso HolidayRate > NRate THEN
        NRate = HolidayRate
        NType = 14
    ENDIF
    IF IsWE1 AndAlso WE1Rate > NRate THEN
        NRate = WE1Rate
        NType = 11
    ENDIF
    IF IsWE2 AndAlso WE2Rate > NRate THEN
        NRate = WE2Rate
        NType = 12
    ENDIF
    IF IsWE3 AndAlso WE3Rate > NRate THEN
        NRate = WE3Rate
        NType = 13
    ENDIF

    DRate = 0
    DType = 0
    IF HasHoliday AndAlso HolidayRate > DRate THEN
        DRate = HolidayRate
        DType = 14
    ENDIF
    IF IsWE1 AndAlso WE1Rate > DRate THEN
        DRate = WE1Rate
        DType = 11
    ENDIF
    IF IsWE2 AndAlso WE2Rate > DRate THEN
        DRate = WE2Rate
        DType = 12
    ENDIF
    IF IsWE3 AndAlso WE3Rate > DRate THEN
        DRate = WE3Rate
        DType = 13
    ENDIF

    Amount = 0
    IF NType = WantType THEN Amount = Amount + NightHours * NRate ENDIF
    IF DType = WantType THEN Amount = Amount + NonNightHours * DRate ENDIF

    SegBonusForType = Amount
ENDFUNCTION

DIM TotalBonus, WeekdayNextDay
DIM BonusNight, BonusWeekend1, BonusWeekend2, BonusWeekend3, BonusHoliday

WeekdayNextDay = (Weekday MOD 7) + 1

IF TimeToHours(UntilHour) = TimeToHours(FromHour) AndAlso Hour <= 0 THEN
    BonusNight = 0
    BonusWeekend1 = 0
    BonusWeekend2 = 0
    BonusWeekend3 = 0
    BonusHoliday = 0
ELSE
    IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
        BonusNight = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 10) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 10)
        BonusWeekend1 = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 11) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 11)
        BonusWeekend2 = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 12) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 12)
        BonusWeekend3 = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 13) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 13)
        BonusHoliday = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 14) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 14)
    ELSE
        BonusNight = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 10)
        BonusWeekend1 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 11)
        BonusWeekend2 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 12)
        BonusWeekend3 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 13)
        BonusHoliday = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 14)
    ENDIF
ENDIF

TotalBonus = BonusNight + BonusWeekend1 + BonusWeekend2 + BonusWeekend3 + BonusHoliday

OUTPUT 1, Round(TotalBonus, 2)
OUTPUT 10, BonusNight
OUTPUT 11, BonusWeekend1
OUTPUT 12, BonusWeekend2
OUTPUT 13, BonusWeekend3
OUTPUT 14, BonusHoliday
";

        private const string OldAllShiftAdditive = @"IMPORT Hour, FromHour, UntilHour
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
";

        private const string NewAllShiftAdditive = @"IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, WE1Rate, WE2Rate, WE3Rate
IMPORT NightStart, NightEnd
IMPORT WeekendDay1, WeekendDay2, WeekendDay3

FUNCTION SegBonusForType(StartTime, EndTime, HolidayFlag, WeekdayNum, WantType)
    DIM SegmentHours, NightHours, Amount
    DIM HasHoliday, IsWE1, IsWE2, IsWE3

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = 0
    IF SegmentHours > 0 THEN NightHours = TimeOverlap(NightStart, NightEnd, StartTime, EndTime) ENDIF

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

IF TimeToHours(UntilHour) = TimeToHours(FromHour) AndAlso Hour <= 0 THEN
    BonusNight = 0
    BonusWeekend1 = 0
    BonusWeekend2 = 0
    BonusWeekend3 = 0
    BonusHoliday = 0
ELSE
    IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
        BonusNight = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 10) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 10)
        BonusWeekend1 = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 11) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 11)
        BonusWeekend2 = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 12) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 12)
        BonusWeekend3 = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 13) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 13)
        BonusHoliday = SegBonusForType(FromHour, ""24:00"", Holiday, Weekday, 14) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 14)
    ELSE
        BonusNight = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 10)
        BonusWeekend1 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 11)
        BonusWeekend2 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 12)
        BonusWeekend3 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 13)
        BonusHoliday = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 14)
    ENDIF
ENDIF

TotalBonus = BonusNight + BonusWeekend1 + BonusWeekend2 + BonusWeekend3 + BonusHoliday

OUTPUT 1, Round(TotalBonus, 2)
OUTPUT 10, BonusNight
OUTPUT 11, BonusWeekend1
OUTPUT 12, BonusWeekend2
OUTPUT 13, BonusWeekend3
OUTPUT 14, BonusHoliday
";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data fix for existing installations: the seeded AllShift and AllShiftAdditive scripts split a shift that
            // reaches midnight into two segments. A segment of zero length (00:00-00:00: the second half of every shift
            // that ends at 00:00, or the first half of one that starts at 00:00) was counted as 1 h of night by TimeOverlap,
            // which reads an equal start and end as a full day, while its own hours are 0. So a shift ending at 00:00
            // earned a phantom hour of night surcharge, and a 24 h shift from 00:00 to 00:00 lost its surcharges. The
            // scripts now skip empty segments, end the first segment at 24:00 and pay nothing for a window of zero hours.
            // Only rows whose content still equals the shipped text are rewritten (line endings compared as LF), so a
            // macro an operator changed is never overwritten. MacrosSeed.cs already ships the final AllShift.
            migrationBuilder.Sql(BuildUpdate(AllShiftName, OldAllShift, NewAllShift));
            migrationBuilder.Sql(BuildUpdate(AllShiftAdditiveName, OldAllShiftAdditive, NewAllShiftAdditive));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BuildUpdate(AllShiftName, NewAllShift, OldAllShift));
            migrationBuilder.Sql(BuildUpdate(AllShiftAdditiveName, NewAllShiftAdditive, OldAllShiftAdditive));
        }

        private static string BuildUpdate(string macroName, string fromContent, string toContent) =>
            "UPDATE public.macro SET content = " + Quote(toContent) + " WHERE name = " + Quote(macroName)
            + " AND replace(content, chr(13), '') = " + Quote(fromContent) + ";";

        private static string Quote(string text) =>
            "'" + text.Replace(CrLf, LineBreak).Replace(CarriageReturn, string.Empty).Replace("'", "''") + "'";
    }
}
