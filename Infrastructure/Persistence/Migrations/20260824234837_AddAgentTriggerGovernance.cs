using System;
using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentTriggerGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_trigger_governance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    max_action = table.Column<int>(type: "integer", nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    responsible_owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    daily_action_budget = table.Column<int>(type: "integer", nullable: false),
                    window_action_limit = table.Column<int>(type: "integer", nullable: false),
                    window_minutes = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_agent_trigger_governance", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_governance_trigger_kind_global",
                table: "agent_trigger_governance",
                column: "trigger_kind",
                unique: true,
                filter: "\"group_id\" IS NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_governance_trigger_kind_group",
                table: "agent_trigger_governance",
                columns: new[] { "trigger_kind", "group_id" },
                unique: true,
                filter: "\"group_id\" IS NOT NULL AND \"is_deleted\" = false");

            AgentTriggerGovernanceDefaultsSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_trigger_governance");
        }
    }
}
