using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentTriggerPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "plan_id",
                table: "skill_selection_trajectories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "agent_trigger_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    muted = table.Column<bool>(type: "boolean", nullable: false),
                    snoozed_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    minimum_severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
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
                    table.PrimaryKey("pk_agent_trigger_preferences", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_plan_id",
                table: "skill_selection_trajectories",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_preferences_user_id_trigger_kind",
                table: "agent_trigger_preferences",
                columns: new[] { "user_id", "trigger_kind" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.AddForeignKey(
                name: "fk_skill_selection_trajectories_agent_plans_plan_id",
                table: "skill_selection_trajectories",
                column: "plan_id",
                principalTable: "agent_plans",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_skill_selection_trajectories_agent_plans_plan_id",
                table: "skill_selection_trajectories");

            migrationBuilder.DropTable(
                name: "agent_trigger_preferences");

            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_plan_id",
                table: "skill_selection_trajectories");

            migrationBuilder.DropColumn(
                name: "plan_id",
                table: "skill_selection_trajectories");
        }
    }
}
