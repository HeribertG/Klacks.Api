using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodCapRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "period_cap_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<int>(type: "integer", nullable: false),
                    cap_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    warn_at_percent = table.Column<int>(type: "integer", nullable: true),
                    custom_period_weeks = table.Column<int>(type: "integer", nullable: true),
                    rolling_window_weeks = table.Column<int>(type: "integer", nullable: true),
                    max_average_weekly_hours = table.Column<decimal>(type: "numeric", nullable: true),
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
                    table.PrimaryKey("pk_period_cap_rule", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_period_cap_rule_import_source_key",
                table: "period_cap_rule",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "period_cap_rule");
        }
    }
}
