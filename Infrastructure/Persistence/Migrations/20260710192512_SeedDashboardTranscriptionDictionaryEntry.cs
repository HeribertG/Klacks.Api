using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedDashboardTranscriptionDictionaryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO transcription_dictionary_entries (id, correct_term, category, phonetic_variants, description, create_time, update_time, is_deleted)
                SELECT gen_random_uuid(), 'Dashboard', NULL,
                       '[""Deutsch-Board"", ""Deutschboard"", ""Däschbord"", ""Deschboard"", ""Dashbord"", ""Dash Board""]'::jsonb,
                       NULL, NOW(), NOW(), false
                WHERE NOT EXISTS (
                    SELECT 1 FROM transcription_dictionary_entries
                    WHERE correct_term = 'Dashboard' AND is_deleted = false
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM transcription_dictionary_entries
                WHERE correct_term = 'Dashboard' AND is_deleted = false;
            ");
        }
    }
}
