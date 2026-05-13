using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentPlansTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "sporadic_status",
                table: "shift_day_assignments",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "agent_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goal = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    steps_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_step_index = table.Column<int>(type: "integer", nullable: false),
                    last_error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
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
                    table.PrimaryKey("pk_agent_plans", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_agent_id_status",
                table: "agent_plans",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_session_id",
                table: "agent_plans",
                column: "session_id",
                filter: "session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_status",
                table: "agent_plans",
                column: "status",
                filter: "status IN ('drafting','executing','paused_for_approval')");

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_user_id",
                table: "agent_plans",
                column: "user_id",
                filter: "user_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_plans");

            migrationBuilder.DropColumn(
                name: "sporadic_status",
                table: "shift_day_assignments");
        }
    }
}
