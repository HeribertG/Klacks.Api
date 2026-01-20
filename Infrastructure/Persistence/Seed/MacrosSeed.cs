using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public class MacrosSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                              @"INSERT INTO public.macro (id,""name"",""content"",""type"",description_de,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted ) VALUES
	 ('3bac9e54-4368-4174-8bc9-435ce08aecbd','Vacation','IF WeekdayNumber  = 1 OR WeekdayNumber  = 7 OR IsHolyday THEN
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
 
Message 1, Hour 
 
 
 
 
 
 ',0,'','2022-07-10 07:06:56.146','admin',NULL,'',NULL,false,'' ),
	 ('ac8a7b05-2312-41aa-a21d-e3edba54aef5','Illness','IF BlockShiftIndex  = 1 THEN
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
 
Message 1, Hour
 
 
 
 
 
 
 
 
 ',0,'','2022-07-10 07:08:53.037','admin',NULL,'',NULL,false,'' ),
	 ('ad86380e-3e8e-4497-95c1-3555ee0803c4','Military Service','IF WeekdayNumber  = 1 OR WeekdayNumber  = 7 OR IsHolyday THEN
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
 
Message 1, Hour 
 
 
 
 
 
 ',0,'','2022-07-10 07:08:53.029','admin',NULL,'',NULL,false,'' ),
	 ('a3edd3f5-c31c-4746-a9a0-c613d14ffd23','AllShift','IMPORT Hour, FromHour, UntilHour
IMPORT Weekday, Holiday, HolidayNextDay
IMPORT NightRate, HolidayRate, SaRate, SoRate

FUNCTION CalcSegment(StartTime, EndTime, HolidayFlag, WeekdayNum)
    DIM SegmentHours, NightHours, NonNightHours
    DIM NRate, DRate, HasHoliday, IsSaturday, IsSunday

    SegmentHours = TimeToHours(EndTime) - TimeToHours(StartTime)
    IF SegmentHours < 0 THEN SegmentHours = SegmentHours + 24 ENDIF

    NightHours = TimeOverlap(""23:00"", ""06:00"", StartTime, EndTime)
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
    TotalBonus = CalcSegment(FromHour, ""00:00"", Holiday, Weekday)
    TotalBonus = TotalBonus + CalcSegment(""00:00"", UntilHour, HolidayNextDay, WeekdayNextDay)
ELSE
    TotalBonus = CalcSegment(FromHour, UntilHour, Holiday, Weekday)
ENDIF

OUTPUT 1, Round(TotalBonus, 2)
',0,'','2022-07-10 07:08:53.043','admin',NULL,'',NULL,false,'' ),
	 ('f7704df2-bb51-40c8-9ecd-ad57c1064490','Null Hour','Dim Hour
Message 1, 0 
 
 
 
 
 
 ',0,'','2022-07-10 07:08:53.051','admin','2022-07-10 07:10:25.553','admin',NULL,false,'' ),
	 ('b1481e19-eaba-458a-a33b-666f2ecc28d2','Accident','IF GuaranteedHours > 0 THEN
	IF GuaranteedHours <> FullTime THEN
		DIM Percent
		Percent = GuaranteedHours / FullTime
		Hour = Hour * Percent
	END IF
ELSE
	Hour = 0
END IF
 
Message 1, Hour
 
 
 
 
 
 
 
 ',0,'','2022-07-10 07:08:53.041','admin','2023-07-16 14:42:01.647','admin',NULL,false,'' );

"
                              );
        }
    }
}
