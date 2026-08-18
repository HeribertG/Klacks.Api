using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationRosterOrderColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "escalation_roster_order",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: alphabetical (last_name, first_name), same starting point as DisplayOrder's
            // own backfill, so drag'n'drop has a sensible order on first load.
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers"" AS u
                SET escalation_roster_order = ranked.row_number
                FROM (
                    SELECT id, ROW_NUMBER() OVER (ORDER BY last_name, first_name) AS row_number
                    FROM ""AspNetUsers""
                    WHERE discriminator = 'AppUser'
                ) AS ranked
                WHERE u.id = ranked.id;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "escalation_roster_order",
                table: "AspNetUsers");
        }
    }
}
