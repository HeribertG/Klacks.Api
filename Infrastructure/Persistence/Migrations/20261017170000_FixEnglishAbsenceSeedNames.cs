using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Corrects the English names of three seeded absence types on existing databases: "Vacancies" (open
    /// positions) becomes "Vacation", "1/2 Vacancies" becomes "1/2 Vacation" and "Indisposed" (the blocked day,
    /// German "Gesperrter Tag") becomes "Blocked day". AbsencesSeed.cs ships the corrected names for fresh
    /// databases. Each row is matched by its fixed seed id AND the old English value, so an English name a
    /// customer already changed stays untouched; re-running is a no-op. Abbreviations are deliberately not
    /// changed, because they are the input shortcuts of the schedule cells. Down restores the old names only
    /// where the corrected value is still in place.
    /// </summary>
    public partial class FixEnglishAbsenceSeedNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.absence SET name = jsonb_set(name, '{en}', '""Vacation""')
                WHERE id = '15ee57e6-31d1-492e-bf83-d3c386ef7472' AND name->>'en' = 'Vacancies';

                UPDATE public.absence SET name = jsonb_set(name, '{en}', '""1/2 Vacation""')
                WHERE id = '53851d0a-ff7f-460a-82a0-481aa3547d7e' AND name->>'en' = '1/2 Vacancies';

                UPDATE public.absence SET name = jsonb_set(name, '{en}', '""Blocked day""')
                WHERE id = '72c43150-bd93-4a41-be99-9d7a603ff596' AND name->>'en' = 'Indisposed';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.absence SET name = jsonb_set(name, '{en}', '""Vacancies""')
                WHERE id = '15ee57e6-31d1-492e-bf83-d3c386ef7472' AND name->>'en' = 'Vacation';

                UPDATE public.absence SET name = jsonb_set(name, '{en}', '""1/2 Vacancies""')
                WHERE id = '53851d0a-ff7f-460a-82a0-481aa3547d7e' AND name->>'en' = '1/2 Vacation';

                UPDATE public.absence SET name = jsonb_set(name, '{en}', '""Indisposed""')
                WHERE id = '72c43150-bd93-4a41-be99-9d7a603ff596' AND name->>'en' = 'Blocked day';");
        }
    }
}
