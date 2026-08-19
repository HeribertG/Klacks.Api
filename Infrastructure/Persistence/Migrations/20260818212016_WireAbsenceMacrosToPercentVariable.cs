using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WireAbsenceMacrosToPercentVariable : Migration
    {
        // Data fix for existing installations, three parts (MacrosSeed.cs already ships the new
        // content for fresh databases; every statement here is idempotent and re-run safe):
        //
        // 1. The seeded absence macros (Vacation, Accident, Accident50%, Military Service) derived the
        //    workload share as GuaranteedHours / FullTime. Once guaranteed hours vary per month
        //    (company-wide monthly target hours, inherited contract hours) that ratio mistakes a
        //    full-time employee for a part-timer and shrinks the absence credit. The macros now scale
        //    by the imported Percent variable fed from the contract workload. Rows are matched by the
        //    fixed seed id OR by name (dev databases accumulate duplicate seeded rows), guarded by the
        //    old ratio expression so customized macros stay untouched.
        //
        // 2. A Vacation50% macro is inserted (analogous to Accident50%) for the seeded half-day
        //    vacation absence type.
        //
        // 3. The seeded absence types are wired to their macros for the first time. Only rows whose
        //    macro_id is still NULL are touched, so customer assignments survive.
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            const string ImportBlock = @"import hour
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
import percent";

            const string OldRatioGuard = "content LIKE '%GuaranteedHours / FullTime%'";

            migrationBuilder.Sql($@"UPDATE public.macro
SET content = '{ImportBlock}

IF Weekday  = 6 OR Weekday  = 7 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour'
WHERE (id = '3bac9e54-4368-4174-8bc9-435ce08aecbd' OR name = 'Vacation')
  AND is_deleted = false
  AND {OldRatioGuard};");

            migrationBuilder.Sql($@"UPDATE public.macro
SET content = '{ImportBlock}

IF Weekday  = 6 OR Weekday  = 7 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour'
WHERE (id = 'ad86380e-3e8e-4497-95c1-3555ee0803c4' OR name = 'Military Service')
  AND is_deleted = false
  AND {OldRatioGuard};");

            migrationBuilder.Sql($@"UPDATE public.macro
SET content = '{ImportBlock}

IF GuaranteedHours > 0 THEN
	Hour = Hour * Percent / 100
ELSE
	Hour = 0
END IF

OUTPUT 1, Hour'
WHERE (id = 'b1481e19-eaba-458a-a33b-666f2ecc28d2' OR name = 'Accident')
  AND is_deleted = false
  AND {OldRatioGuard};");

            migrationBuilder.Sql($@"UPDATE public.macro
SET content = '{ImportBlock}

IF GuaranteedHours > 0 THEN
	Hour = Hour * Percent / 100
	Hour = Hour / 2
ELSE
	Hour = 0
END IF

OUTPUT 1, Hour'
WHERE (id = 'ac8a7b05-2312-41aa-a21d-e3edba54aef5' OR name = 'Accident50%')
  AND is_deleted = false
  AND {OldRatioGuard};");

            migrationBuilder.Sql($@"INSERT INTO public.macro (id,""name"",""content"",""type"",description,create_time,current_user_created,update_time,current_user_updated,deleted_time,is_deleted,current_user_deleted,category)
SELECT '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43','Vacation50%','{ImportBlock}

IF Weekday  = 6 OR Weekday  = 7 OR Holiday THEN
	Hour = 0
ELSE
	IF GuaranteedHours > 0 THEN
		Hour = Hour * Percent / 100
		Hour = Hour / 2
	ELSE
		Hour = 0
	END IF
END IF

OUTPUT 1, Hour',0,'{{""de"": null, ""en"": null, ""fr"": null, ""it"": null}}','2026-08-18 00:00:00.000','admin',NULL,'',NULL,false,'',0
WHERE NOT EXISTS (
    SELECT 1 FROM public.macro
    WHERE id = '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43'
       OR (name = 'Vacation50%' AND is_deleted = false));");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = '3bac9e54-4368-4174-8bc9-435ce08aecbd'
WHERE id = '15ee57e6-31d1-492e-bf83-d3c386ef7472' AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = '3bac9e54-4368-4174-8bc9-435ce08aecbd' AND m.is_deleted = false);");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43'
WHERE id = '53851d0a-ff7f-460a-82a0-481aa3547d7e' AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43' AND m.is_deleted = false);");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = 'b1481e19-eaba-458a-a33b-666f2ecc28d2'
WHERE id = '1070d7e6-f314-4d20-bc18-98c5357a4f89' AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = 'b1481e19-eaba-458a-a33b-666f2ecc28d2' AND m.is_deleted = false);");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = 'ac8a7b05-2312-41aa-a21d-e3edba54aef5'
WHERE id = '1d5a1964-7fad-4da9-945c-3ad00c0edaa8' AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = 'ac8a7b05-2312-41aa-a21d-e3edba54aef5' AND m.is_deleted = false);");

            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = 'ad86380e-3e8e-4497-95c1-3555ee0803c4'
WHERE id = 'a04f8e87-8966-47c0-b293-931ea4f949ae' AND macro_id IS NULL AND is_deleted = false
  AND EXISTS (SELECT 1 FROM public.macro m WHERE m.id = 'ad86380e-3e8e-4497-95c1-3555ee0803c4' AND m.is_deleted = false);");
        }

        // Unwiring the absence types restores the pre-macro behaviour (no automatic break work time).
        // The macro contents are deliberately not restored: re-inserting the month-dependent ratio
        // formula has no rollback value, and the Percent import stays functional either way.
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"UPDATE public.absence SET macro_id = NULL
WHERE id IN (
    '15ee57e6-31d1-492e-bf83-d3c386ef7472',
    '53851d0a-ff7f-460a-82a0-481aa3547d7e',
    '1070d7e6-f314-4d20-bc18-98c5357a4f89',
    '1d5a1964-7fad-4da9-945c-3ad00c0edaa8',
    'a04f8e87-8966-47c0-b293-931ea4f949ae');");
        }
    }
}
