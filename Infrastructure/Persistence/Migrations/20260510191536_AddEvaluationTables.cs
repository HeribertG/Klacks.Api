using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "eval_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goldset = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    composite_score = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    dimensions_json = table.Column<string>(type: "jsonb", nullable: false),
                    regression_vs_baseline = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    items_total = table.Column<int>(type: "integer", nullable: false),
                    items_passed = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_eval_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_selection_trajectories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    turn_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    user_message_hash = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    intent_excerpt = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    knowledge_index_candidates_json = table.Column<string>(type: "jsonb", nullable: false),
                    llm_chosen_skill = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    was_executed = table.Column<bool>(type: "boolean", nullable: false),
                    was_corrected = table.Column<bool>(type: "boolean", nullable: false),
                    correction_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    latency_ms_total = table.Column<int>(type: "integer", nullable: false),
                    latency_ms_knowledge = table.Column<int>(type: "integer", nullable: false),
                    latency_ms_llm = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_skill_selection_trajectories", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_eval_runs_goldset_create_time",
                table: "eval_runs",
                columns: new[] { "goldset", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_agent_id_create_time",
                table: "skill_selection_trajectories",
                columns: new[] { "agent_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_was_corrected",
                table: "skill_selection_trajectories",
                column: "was_corrected",
                filter: "\"was_corrected\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eval_runs");

            migrationBuilder.DropTable(
                name: "skill_selection_trajectories");
        }
    }
}
