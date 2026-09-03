// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public class MacrosSeed
    {
        // Every row is its own guarded INSERT ... SELECT ... WHERE NOT EXISTS statement, matching the
        // idiom migrations already use (WireAbsenceMacrosToPercentVariable.cs,
        // SplitTrainingAndWirePaidAbsence.cs), so this seed stays safe to run after a migration that
        // already inserted the same fixed-id row: WireAbsenceMacrosToPercentVariable and
        // SplitTrainingAndWirePaidAbsence seed 'Vacation50%' and 'Paid Absence' with the same ids used
        // below. Without the guard, a fresh install crashes with Postgres 23505 duplicate key on
        // pk_macro, because migrations always run before this seed.
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT 'b1481e19-eaba-458a-a33b-666f2ecc28d2','Accident','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime
import percent

IF GuaranteedHours > 0 THEN
	Hour = Hour * Percent / 100
ELSE
	Hour = 0
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = 'b1481e19-eaba-458a-a33b-666f2ecc28d2');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT 'ac8a7b05-2312-41aa-a21d-e3edba54aef5','Accident50%','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime
import percent

IF GuaranteedHours > 0 THEN
	Hour = Hour * Percent / 100
	Hour = Hour / 2
ELSE
	Hour = 0
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = 'ac8a7b05-2312-41aa-a21d-e3edba54aef5');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23','AllShift','IMPORT Hour, FromHour, UntilHour
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
',1,'{""de"":""""}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'',1
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT 'ad86380e-3e8e-4497-95c1-3555ee0803c4','Military Service','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime
import percent
import weekendday1
import weekendday2
import weekendday3

IF Weekday = WeekendDay1 OR Weekday = WeekendDay2 OR Weekday = WeekendDay3 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = 'ad86380e-3e8e-4497-95c1-3555ee0803c4');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT 'f7704df2-bb51-40c8-9ecd-ad57c1064490','Null Hour','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime

OUTPUT 1, 0',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = 'f7704df2-bb51-40c8-9ecd-ad57c1064490');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT '3bac9e54-4368-4174-8bc9-435ce08aecbd','Vacation','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime
import percent
import weekendday1
import weekendday2
import weekendday3

IF Weekday = WeekendDay1 OR Weekday = WeekendDay2 OR Weekday = WeekendDay3 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = '3bac9e54-4368-4174-8bc9-435ce08aecbd');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43','Vacation50%','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime
import percent
import weekendday1
import weekendday2
import weekendday3

IF Weekday = WeekendDay1 OR Weekday = WeekendDay2 OR Weekday = WeekendDay3 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
		Hour = Hour / 2
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2026-08-18 00:00:00.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43');");

            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT '9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31','Paid Absence','import hour
import fromhour
import untilhour
import weekday
import holiday
import holidaynextday
import nightrate
import holidayrate
import we1rate
import we2rate
import guaranteedhours
import fulltime
import percent

DIM Duration
Duration = TimeToHours(UntilHour) - TimeToHours(FromHour)
IF Duration < 0 THEN
	Duration = Duration + 24
END IF
IF Duration >= 23.9 THEN
	Duration = 0
END IF

OUTPUT 1, Duration',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2026-08-19 00:00:00.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (SELECT 1 FROM public.macro WHERE id = '9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31');");
        }
    }
}
