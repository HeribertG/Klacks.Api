using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHeartbeatConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "heartbeat_configs");

            // The assistant heartbeat is gone; AgentTriggerBackgroundService (the proactive trigger
            // pipeline) is the only heartbeat left. skill-seeds.json no longer defines the skill, but
            // the seed loader only upserts and never prunes, so the already-seeded catalog row and its
            // phrases would survive as an entry pointing at a handler class that no longer exists.
            // agent_skill_executions cascade with the skill row; skill_phrase is addressed by name and
            // has no foreign key, so it is deleted explicitly.
            migrationBuilder.Sql(@"
                DELETE FROM skill_phrase
                WHERE owner_kind = 'Skill' AND owner_name = 'configure_heartbeat';");

            migrationBuilder.Sql(@"
                DELETE FROM agent_skills
                WHERE name = 'configure_heartbeat';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The table is restored empty, and the deleted seed rows are deliberately not restored:
            // skill-seeds.json is the source of truth for the skill catalog and no longer defines
            // configure_heartbeat, so re-inserting it would recreate a row without a handler.
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
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateIndex(
                name: "ix_heartbeat_configs_user_id",
                table: "heartbeat_configs",
                column: "user_id",
                unique: true,
                filter: "\"is_deleted\" = false");
        }
    }
}
