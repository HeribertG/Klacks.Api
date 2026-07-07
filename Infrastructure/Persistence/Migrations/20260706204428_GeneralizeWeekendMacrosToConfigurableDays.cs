using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GeneralizeWeekendMacrosToConfigurableDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE public.macro SET content = $macro$IMPORT Hour, FromHour, UntilHour
                IMPORT Weekday, Holiday, HolidayNextDay
                IMPORT NightRate, HolidayRate, SaRate, SoRate
                IMPORT WeekendDay1, WeekendDay2

                FUNCTION CalcSegment(StartTime, EndTime, HolidayFlag, WeekdayNum)
                    DIM SegmentHours, NightHours, NonNightHours
                    DIM NRate, DRate, HasHoliday, IsWeekendDay1, IsWeekendDay2

                    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
                    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

                    NightHours = TimeOverlap("23:00", "06:00", StartTime, EndTime)
                    NonNightHours = SegmentHours - NightHours

                    HasHoliday = HolidayFlag = 1
                    IsWeekendDay1 = WeekdayNum = WeekendDay1
                    IsWeekendDay2 = WeekdayNum = WeekendDay2

                    NRate = 0
                    IF NightHours > 0 THEN NRate = NightRate ENDIF
                    IF HasHoliday AndAlso HolidayRate > NRate THEN NRate = HolidayRate ENDIF
                    IF IsWeekendDay1 AndAlso SaRate > NRate THEN NRate = SaRate ENDIF
                    IF IsWeekendDay2 AndAlso SoRate > NRate THEN NRate = SoRate ENDIF

                    DRate = 0
                    IF HasHoliday AndAlso HolidayRate > DRate THEN DRate = HolidayRate ENDIF
                    IF IsWeekendDay1 AndAlso SaRate > DRate THEN DRate = SaRate ENDIF
                    IF IsWeekendDay2 AndAlso SoRate > DRate THEN DRate = SoRate ENDIF

                    CalcSegment = NightHours * NRate + NonNightHours * DRate
                ENDFUNCTION

                DIM TotalBonus, WeekdayNextDay

                WeekdayNextDay = (Weekday MOD 7) + 1

                IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
                    TotalBonus = CalcSegment(FromHour, "00:00", Holiday, Weekday)
                    TotalBonus = TotalBonus + CalcSegment("00:00", UntilHour, HolidayNextDay, WeekdayNextDay)
                ELSE
                    TotalBonus = CalcSegment(FromHour, UntilHour, Holiday, Weekday)
                ENDIF

                OUTPUT 1, Round(TotalBonus, 2)
                $macro$
                WHERE id = 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23';
                """);

            migrationBuilder.Sql("""
                UPDATE public.macro SET content = $macro$import hour
                import fromhour
                import untilhour
                import weekday
                import holiday
                import holidaynextday
                import nightrate
                import holidayrate
                import sarate
                import sorate
                import guaranteedhours
                import fulltime
                import weekendday1
                import weekendday2

                IF Weekday  = WeekendDay1 OR Weekday  = WeekendDay2 OR Holiday THEN
                	Hour= 0
                ELSE
                	IF GuaranteedHours > 0 THEN
                		IF GuaranteedHours <> FullTime THEN
                			DIM Percent
                			Percent = GuaranteedHours / FullTime
                			Hour = Hour * Percent
                		END IF
                	ELSE
                		Hour = 0
                	END IF
                END IF

                OUTPUT 1, Hour$macro$
                WHERE id = 'ad86380e-3e8e-4497-95c1-3555ee0803c4';
                """);

            migrationBuilder.Sql("""
                UPDATE public.macro SET content = $macro$import hour
                import fromhour
                import untilhour
                import weekday
                import holiday
                import holidaynextday
                import nightrate
                import holidayrate
                import sarate
                import sorate
                import guaranteedhours
                import fulltime
                import weekendday1
                import weekendday2

                IF Weekday  = WeekendDay1 OR Weekday  = WeekendDay2 OR Holiday THEN
                	Hour= 0
                ELSE
                	IF GuaranteedHours > 0 THEN
                		IF GuaranteedHours <> FullTime THEN
                			DIM Percent
                			Percent = GuaranteedHours / FullTime
                			Hour = Hour * Percent
                		ELSE
                			Hour = Hour
                		END IF
                	ELSE
                		Hour = 0
                	END IF
                END IF

                OUTPUT 1, Hour$macro$
                WHERE id = '3bac9e54-4368-4174-8bc9-435ce08aecbd';
                """);

            migrationBuilder.Sql("""
                INSERT INTO public.settings (id, type, value) VALUES
                ('a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4d10', 'CALENDAR_WEEKEND_DAYS', 'Saturday,Sunday'),
                ('a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4d11', 'CALENDAR_WEEK_START_DAY', 'Monday')
                ON CONFLICT (id) DO NOTHING;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE public.macro SET content = $macro$IMPORT Hour, FromHour, UntilHour
                IMPORT Weekday, Holiday, HolidayNextDay
                IMPORT NightRate, HolidayRate, SaRate, SoRate

                FUNCTION CalcSegment(StartTime, EndTime, HolidayFlag, WeekdayNum)
                    DIM SegmentHours, NightHours, NonNightHours
                    DIM NRate, DRate, HasHoliday, IsSaturday, IsSunday

                    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
                    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

                    NightHours = TimeOverlap("23:00", "06:00", StartTime, EndTime)
                    NonNightHours = SegmentHours - NightHours

                    HasHoliday = HolidayFlag = 1
                    IsSaturday = WeekdayNum = 6
                    IsSunday = WeekdayNum = 7

                    NRate = 0
                    IF NightHours > 0 THEN NRate = NightRate ENDIF
                    IF HasHoliday AndAlso HolidayRate > NRate THEN NRate = HolidayRate ENDIF
                    IF IsSaturday AndAlso SaRate > NRate THEN NRate = SaRate ENDIF
                    IF IsSunday AndAlso SoRate > NRate THEN NRate = SoRate ENDIF

                    DRate = 0
                    IF HasHoliday AndAlso HolidayRate > DRate THEN DRate = HolidayRate ENDIF
                    IF IsSaturday AndAlso SaRate > DRate THEN DRate = SaRate ENDIF
                    IF IsSunday AndAlso SoRate > DRate THEN DRate = SoRate ENDIF

                    CalcSegment = NightHours * NRate + NonNightHours * DRate
                ENDFUNCTION

                DIM TotalBonus, WeekdayNextDay

                WeekdayNextDay = (Weekday MOD 7) + 1

                IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
                    TotalBonus = CalcSegment(FromHour, "00:00", Holiday, Weekday)
                    TotalBonus = TotalBonus + CalcSegment("00:00", UntilHour, HolidayNextDay, WeekdayNextDay)
                ELSE
                    TotalBonus = CalcSegment(FromHour, UntilHour, Holiday, Weekday)
                ENDIF

                OUTPUT 1, Round(TotalBonus, 2)
                $macro$
                WHERE id = 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23';
                """);

            migrationBuilder.Sql("""
                UPDATE public.macro SET content = $macro$import hour
                import fromhour
                import untilhour
                import weekday
                import holiday
                import holidaynextday
                import nightrate
                import holidayrate
                import sarate
                import sorate
                import guaranteedhours
                import fulltime

                IF Weekday  = 6 OR Weekday  = 7 OR Holiday THEN
                	Hour= 0
                ELSE
                	IF GuaranteedHours > 0 THEN
                		IF GuaranteedHours <> FullTime THEN
                			DIM Percent
                			Percent = GuaranteedHours / FullTime
                			Hour = Hour * Percent
                		END IF
                	ELSE
                		Hour = 0
                	END IF
                END IF

                OUTPUT 1, Hour$macro$
                WHERE id = 'ad86380e-3e8e-4497-95c1-3555ee0803c4';
                """);

            migrationBuilder.Sql("""
                UPDATE public.macro SET content = $macro$import hour
                import fromhour
                import untilhour
                import weekday
                import holiday
                import holidaynextday
                import nightrate
                import holidayrate
                import sarate
                import sorate
                import guaranteedhours
                import fulltime

                IF Weekday  = 6 OR Weekday  = 7 OR Holiday THEN
                	Hour= 0
                ELSE
                	IF GuaranteedHours > 0 THEN
                		IF GuaranteedHours <> FullTime THEN
                			DIM Percent
                			Percent = GuaranteedHours / FullTime
                			Hour = Hour * Percent
                		ELSE
                			Hour = Hour
                		END IF
                	ELSE
                		Hour = 0
                	END IF
                END IF

                OUTPUT 1, Hour$macro$
                WHERE id = '3bac9e54-4368-4174-8bc9-435ce08aecbd';
                """);

            migrationBuilder.Sql("""
                DELETE FROM public.settings
                WHERE id IN ('a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4d10', 'a1b2c3d4-e5f6-4a5b-8c9d-0e1f2a3b4d11');
                """);
        }
    }
}
