using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks_api.Data.Seed
{
  public class Macros
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
	 ('a3edd3f5-c31c-4746-a9a0-c613d14ffd23','AllShift','DIM Addhour
Addhour=0
 
IF ShiftType = 3 THEN
	Addhour = Nighthour * NighthourAdditionalPercent
	
	IF WeekdayNumber  = 1 THEN
		Addhour += Hour_befor_Night * NighthourAdditionalPercent
	END IF
 	IF  WeekdayNumber  = 7 AND IsNextDayHolyday = FALSE THEN 
 		Addhour += Hour_after_Night * HolydayhourAdditionalPercent
	END IF
 
 	IF IsNextDayHolyday THEN
		IF  WeekdayNumber  = 1 THEN
			Addhour += Hour_after_Night * HolydayhourAdditionalPercent
		END IF
		IF  WeekdayNumber  <> 1 THEN
			Addhour += Hour_befor_Night * HolydayhourAdditionalPercent
		END IF
 
	END IF
 
	IF IsHolyday AND WeekdayNumber  <> 1 THEN
 		Addhour += Hour_befor_Night * HolydayhourAdditionalPercent
	END IF
Else
	IF  IsHolyday THEN
		Addhour= HourExact * HolydayhourAdditionalPercent
	END IF
	IF WeekdayNumber  = 1 THEN
		Addhour = HourExact * HolydayhourAdditionalPercent
	END IF
END IF
 
Hour += Addhour 
Message 1, hour  
 
 
 
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
