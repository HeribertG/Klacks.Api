// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public class MacrosSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted) VALUES
('b1481e19-eaba-458a-a33b-666f2ecc28d2','Accident','import hour
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

IF GuaranteedHours > 0 THEN
	IF GuaranteedHours <> FullTime THEN
		DIM Percent
		Percent = GuaranteedHours / FullTime
		Hour = Hour * Percent
	END IF
ELSE
	Hour = 0
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,''),
('ac8a7b05-2312-41aa-a21d-e3edba54aef5','Accident50%','import hour
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

IF GuaranteedHours > 0 THEN
	IF GuaranteedHours <> FullTime THEN
		DIM Percent
		Percent = GuaranteedHours / FullTime
		Hour = Hour * Percent
        Hour = Hour/2
	END IF
ELSE
	Hour = 0
END IF

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,''),
('a3edd3f5-c31c-4746-a9a0-c613d14ffd23','AllShift','IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, WE1Rate, WE2Rate

FUNCTION SegBonusForType(StartTime, EndTime, HolidayFlag, WeekdayNum, WantType)
    DIM SegmentHours, NightHours, NonNightHours, Amount
    DIM NRate, DRate, NType, DType, HasHoliday, IsSaturday, IsSunday

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = TimeOverlap(""23:00"", ""06:00"", StartTime, EndTime)
    NonNightHours = SegmentHours - NightHours

    HasHoliday = HolidayFlag = 1
    IsSaturday = WeekdayNum = 6
    IsSunday = WeekdayNum = 7

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
    IF IsSaturday AndAlso WE1Rate > NRate THEN
        NRate = WE1Rate
        NType = 11
    ENDIF
    IF IsSunday AndAlso WE2Rate > NRate THEN
        NRate = WE2Rate
        NType = 12
    ENDIF

    DRate = 0
    DType = 0
    IF HasHoliday AndAlso HolidayRate > DRate THEN
        DRate = HolidayRate
        DType = 14
    ENDIF
    IF IsSaturday AndAlso WE1Rate > DRate THEN
        DRate = WE1Rate
        DType = 11
    ENDIF
    IF IsSunday AndAlso WE2Rate > DRate THEN
        DRate = WE2Rate
        DType = 12
    ENDIF

    Amount = 0
    IF NType = WantType THEN Amount = Amount + NightHours * NRate ENDIF
    IF DType = WantType THEN Amount = Amount + NonNightHours * DRate ENDIF

    SegBonusForType = Amount
ENDFUNCTION

DIM TotalBonus, WeekdayNextDay
DIM BonusNight, BonusWeekend1, BonusWeekend2, BonusHoliday

WeekdayNextDay = (Weekday MOD 7) + 1

IF TimeToHours(UntilHour) <= TimeToHours(FromHour) THEN
    BonusNight = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 10) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 10)
    BonusWeekend1 = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 11) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 11)
    BonusWeekend2 = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 12) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 12)
    BonusHoliday = SegBonusForType(FromHour, ""00:00"", Holiday, Weekday, 14) + SegBonusForType(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay, 14)
ELSE
    BonusNight = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 10)
    BonusWeekend1 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 11)
    BonusWeekend2 = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 12)
    BonusHoliday = SegBonusForType(FromHour, UntilHour, Holiday, Weekday, 14)
ENDIF

TotalBonus = BonusNight + BonusWeekend1 + BonusWeekend2 + BonusHoliday

OUTPUT 1, Round(TotalBonus, 2)
OUTPUT 10, BonusNight
OUTPUT 11, BonusWeekend1
OUTPUT 12, BonusWeekend2
OUTPUT 14, BonusHoliday
',0,'{""de"":""""}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,''),
('ad86380e-3e8e-4497-95c1-3555ee0803c4','Military Service','import hour
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

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,''),
('f7704df2-bb51-40c8-9ecd-ad57c1064490','Null Hour','import hour
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

Dim Hour
OUTPUT 1, 0',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,''),
('3bac9e54-4368-4174-8bc9-435ce08aecbd','Vacation','import hour
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

OUTPUT 1, Hour',0,'{""de"": null, ""en"": null, ""fr"": null, ""it"": null}','2022-07-10 07:08:53.000','admin',NULL,'',NULL,false,'');
");
        }
    }
}
