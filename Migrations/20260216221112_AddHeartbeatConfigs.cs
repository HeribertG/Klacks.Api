using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHeartbeatConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "heartbeat_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    active_hours_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    active_hours_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    checklist_json = table.Column<string>(type: "text", nullable: false),
                    last_executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    onboarding_completed = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_heartbeat_configs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_heartbeat_configs_is_deleted_is_enabled",
                table: "heartbeat_configs",
                columns: new[] { "is_deleted", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_heartbeat_configs_is_deleted_user_id",
                table: "heartbeat_configs",
                columns: new[] { "is_deleted", "user_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "heartbeat_configs");
        }
    }
}
