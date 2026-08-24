using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentConditionLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_conditions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    fingerprint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    detected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    handling_kind = table.Column<int>(type: "integer", nullable: false),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_attempt_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    escalated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reject_reason = table.Column<int>(type: "integer", nullable: true),
                    rejected_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    caused_by_condition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    delegated_max_action = table.Column<int>(type: "integer", nullable: true),
                    delegated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_agent_conditions", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_conditions_agent_conditions_caused_by_condition_id",
                        column: x => x.caused_by_condition_id,
                        principalTable: "agent_conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_agent_conditions_analyse_scenarios_scenario_id",
                        column: x => x.scenario_id,
                        principalTable: "analyse_scenarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_condition_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    detail = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_agent_condition_events", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_condition_events_agent_conditions_condition_id",
                        column: x => x.condition_id,
                        principalTable: "agent_conditions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_agent_condition_events_condition_id",
                table: "agent_condition_events",
                column: "condition_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_conditions_caused_by_condition_id",
                table: "agent_conditions",
                column: "caused_by_condition_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_conditions_fingerprint",
                table: "agent_conditions",
                column: "fingerprint",
                unique: true,
                filter: "\"is_deleted\" = false AND \"status\" NOT IN (3, 4, 5, 6)");

            migrationBuilder.CreateIndex(
                name: "ix_agent_conditions_group_id",
                table: "agent_conditions",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_conditions_scenario_id",
                table: "agent_conditions",
                column: "scenario_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_conditions_status_trigger_kind",
                table: "agent_conditions",
                columns: new[] { "status", "trigger_kind" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agent_condition_events");

            migrationBuilder.DropTable(
                name: "agent_conditions");
        }
    }
}
