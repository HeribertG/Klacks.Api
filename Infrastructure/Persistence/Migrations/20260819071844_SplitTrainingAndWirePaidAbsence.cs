using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitTrainingAndWirePaidAbsence : Migration
    {
        // Continuation of WireAbsenceMacrosToPercentVariable as a separate migration because that one
        // is already applied on existing databases. Four parts, all idempotent and re-run safe
        // (MacrosSeed.cs / AbsencesSeed.cs already ship the final state for fresh databases):
        //
        // 1. The absence macros with weekend logic (Vacation, Vacation50%, Military Service) zeroed
        //    Saturday(6)/Sunday(7) hardcoded. They now compare against the configured weekend days
        //    (imports weekendday1/2/3, ISO numbers, unused slots are 0 and can never match a real
        //    weekday of 1-7) - the same mechanism the AllShift macro already uses. Guarded by BOTH the
        //    known percent-style content AND the hardcoded weekend expression, so customized macros
        //    stay untouched. Accident and Accident50% deliberately have no weekend logic (illness
        //    credits every calendar day) and are not touched.
        //
        // 2. A "Paid Absence" macro is inserted (owner ruling 2026-08-19): it credits exactly the
        //    recorded time span (midnight wrap included) and nothing else. The schedule books
        //    full-day absences as 00:00-23:59, so any span of 23.9 hours or more counts as the
        //    full-day marker and credits 0 - per owner ruling a paid break/training without concrete
        //    times credits no hours.
        //
        // 3. The seeded "Schulung" absence type is split (owner ruling 2026-08-19): the existing row
        //    is renamed to "Schulung bezahlt" (id kept so existing bookings stay valid; guarded by
        //    the old German name so a customer rename survives) and a new "Schulung unbezahlt" row is
        //    inserted.
        //
        // 4. Five more types are wired (only rows whose macro_id is still NULL): blocked day, unpaid
        //    break and unpaid training to Null Hour (zero credit); paid break and paid training to
        //    Paid Absence. On-call types (Pikett) deliberately stay unwired: their compensation is
        //    country and industry specific and is configured by the customer.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string WeekendImportBlock = @"import hour
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
import weekendday3";

            const string PercentStyleGuard = "content LIKE '%Hour = Hour * Percent / 100%'";
            const string HardcodedWeekendGuard = "content LIKE '%Weekday  = 6 OR Weekday  = 7%'";

            migrationBuilder.Sql($@"UPDATE public.macro
SET content = '{WeekendImportBlock}

IF Weekday = WeekendDay1 OR Weekday = WeekendDay2 OR Weekday = WeekendDay3 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour'
WHERE (id IN ('3bac9e54-4368-4174-8bc9-435ce08aecbd', 'ad86380e-3e8e-4497-95c1-3555ee0803c4')
       OR name IN ('Vacation', 'Military Service'))
  AND is_deleted = false
  AND {PercentStyleGuard}
  AND {HardcodedWeekendGuard};");

            migrationBuilder.Sql($@"UPDATE public.macro
SET content = '{WeekendImportBlock}

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

OUTPUT 1, Hour'
WHERE (id = '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43' OR name = 'Vacation50%')
  AND is_deleted = false
  AND {PercentStyleGuard}
  AND {HardcodedWeekendGuard};");

            migrationBuilder.Sql(@"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
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
WHERE NOT EXISTS (
    SELECT 1 FROM public.macro
    WHERE id = '9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31'
       OR (name = 'Paid Absence' AND is_deleted = false));");

            migrationBuilder.Sql(@"UPDATE public.absence
SET name = '{""de"":""Schulung bezahlt"",""en"":""Paid training"",""fr"":""Formation payée"",""it"":""Formazione retribuita""}'
WHERE id = 'dca0367a-530e-4eae-be34-b19d2262587e' AND is_deleted = false
  AND name->>'de' = 'Schulung';");

            migrationBuilder.Sql(@"INSERT INTO public.absence (id,color,default_length,default_value,description,hide_in_gantt,name,abbreviation,undeletable,with_holiday,with_saturday,with_sunday,applies_to_container,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,is_unpaid)
SELECT '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62','#ff80c0',1,1.0,'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}',false,'{""de"":""Schulung unbezahlt"",""en"":""Unpaid training"",""fr"":""Formation non payée"",""it"":""Formazione non retribuita""}','{""de"":""Sc-"",""en"":""Tr-"",""fr"":""Fo-"",""it"":""Fo-""}',false,true,true,true,false,'2026-08-19 00:00:00.000','admin','2026-08-19 00:00:00.000','admin',NULL,false,'',false
WHERE NOT EXISTS (
    SELECT 1 FROM public.absence
    WHERE id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62'
       OR (name->>'de' = 'Schulung unbezahlt' AND is_deleted = false));");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = 'f7704df2-bb51-40c8-9ecd-ad57c1064490'
WHERE id IN ('72c43150-bd93-4a41-be99-9d7a603ff596', 'b8e3c5a1-2d4f-4e6b-9c8a-1f2e3d4c5b6a', '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62')
  AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = 'f7704df2-bb51-40c8-9ecd-ad57c1064490' AND m.is_deleted = false);");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = '9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31'
WHERE id IN ('c9f4d6b2-3e5a-4f7c-ad9b-2a3f4e5d6c7b', 'dca0367a-530e-4eae-be34-b19d2262587e')
  AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = '9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31' AND m.is_deleted = false);");
        }

        // Unwiring restores the pre-macro behaviour for the five types wired here; the weekend rework
        // and the Paid Absence macro are deliberately not reverted (re-inserting the hardcoded
        // Saturday/Sunday check has no rollback value). The training split is reverted: the rename
        // goes back (guarded by the new name) and the inserted unpaid-training row is removed only
        // while nothing references it.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = NULL
WHERE id IN (
    '72c43150-bd93-4a41-be99-9d7a603ff596',
    'b8e3c5a1-2d4f-4e6b-9c8a-1f2e3d4c5b6a',
    '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62',
    'c9f4d6b2-3e5a-4f7c-ad9b-2a3f4e5d6c7b',
    'dca0367a-530e-4eae-be34-b19d2262587e');");

            migrationBuilder.Sql(@"UPDATE public.absence
SET name = '{""de"":""Schulung"",""en"":""Training"",""fr"":""Formation"",""it"":""Formazione""}'
WHERE id = 'dca0367a-530e-4eae-be34-b19d2262587e' AND is_deleted = false
  AND name->>'de' = 'Schulung bezahlt';");

            migrationBuilder.Sql(@"DELETE FROM public.absence
WHERE id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62'
  AND NOT EXISTS (SELECT 1 FROM public.break b WHERE b.absence_id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62')
  AND NOT EXISTS (SELECT 1 FROM public.break_placeholder bp WHERE bp.absence_id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62')
  AND NOT EXISTS (SELECT 1 FROM public.absence_detail ad WHERE ad.absence_id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62')
  AND NOT EXISTS (SELECT 1 FROM public.container_template_item cti WHERE cti.absence_id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62')
  AND NOT EXISTS (SELECT 1 FROM public.container_shift_override_items csoi WHERE csoi.absence_id = '4a7e2c91-6b3f-4d58-9e0a-1c5b8f7d3a62');");
        }
    }
}
