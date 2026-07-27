using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOnCallData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM work_change WHERE type IN (10, 11);
                DELETE FROM settings WHERE type LIKE 'WORKTIME_ONCALL%';
                DELETE FROM navigation_target_synonyms WHERE target_id = 'on-call';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: the deleted rows are on-call WorkChanges (type 10/11) and
            // WORKTIME_ONCALL_* settings from the removed K17 OnCall feature (wrong business definition,
            // fully deleted from the codebase) — there is nothing meaningful to restore.
        }
    }
}
