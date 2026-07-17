using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestrictedTimeWindowRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "restricted_time_window_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_from_month = table.Column<int>(type: "integer", nullable: false),
                    season_from_day = table.Column<int>(type: "integer", nullable: false),
                    season_to_month = table.Column<int>(type: "integer", nullable: false),
                    season_to_day = table.Column<int>(type: "integer", nullable: false),
                    daily_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    daily_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    applies_to_group_tag = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    import_source_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    import_content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_restricted_time_window_rule", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_restricted_time_window_rule_import_source_key",
                table: "restricted_time_window_rule",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "restricted_time_window_rule");
        }
    }
}
