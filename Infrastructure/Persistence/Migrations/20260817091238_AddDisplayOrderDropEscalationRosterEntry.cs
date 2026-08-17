using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisplayOrderDropEscalationRosterEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "escalation_roster_entries");

            migrationBuilder.AddColumn<int>(
                name: "display_order",
                table: "AspNetUsers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: reproduce the prior client-side alphabetical sort (last_name, first_name) as
            // the starting DisplayOrder, so drag'n'drop has a sensible order on first load instead of
            // every existing user starting at 0.
            migrationBuilder.Sql(@"
                UPDATE ""AspNetUsers"" AS u
                SET display_order = ranked.row_number
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
                name: "display_order",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "escalation_roster_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    derived_rank = table.Column<int>(type: "integer", nullable: true),
                    group_root_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    is_orphaned = table.Column<bool>(type: "boolean", nullable: false),
                    override_rank = table.Column<int>(type: "integer", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_escalation_roster_entries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_escalation_roster_entries_group_root_id_user_id",
                table: "escalation_roster_entries",
                columns: new[] { "group_root_id", "user_id" },
                unique: true);
        }
    }
}
