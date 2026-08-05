using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnswerGroundingShadowTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "answer_grounding_daily_counters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluator_version = table.Column<int>(type: "integer", nullable: false),
                    turns_evaluated = table.Column<int>(type: "integer", nullable: false),
                    turns_clean = table.Column<int>(type: "integer", nullable: false),
                    turns_with_findings = table.Column<int>(type: "integer", nullable: false),
                    turns_skipped = table.Column<int>(type: "integer", nullable: false),
                    turns_no_verdict = table.Column<int>(type: "integer", nullable: false),
                    claims_extracted = table.Column<int>(type: "integer", nullable: false),
                    claims_uncovered = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_answer_grounding_daily_counters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "answer_grounding_findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "text", nullable: true),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    evaluator_version = table.Column<int>(type: "integer", nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false),
                    claims_extracted = table.Column<int>(type: "integer", nullable: false),
                    claims_uncovered = table.Column<int>(type: "integer", nullable: false),
                    uncovered_claims_json = table.Column<string>(type: "text", nullable: false),
                    response_excerpt = table.Column<string>(type: "text", nullable: false),
                    evidence_excerpt = table.Column<string>(type: "text", nullable: false),
                    empty_data_despite_success = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_answer_grounding_findings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_answer_grounding_daily_counters_day_agent_id_evaluator_vers",
                table: "answer_grounding_daily_counters",
                columns: new[] { "day", "agent_id", "evaluator_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_answer_grounding_findings_agent_id_create_time",
                table: "answer_grounding_findings",
                columns: new[] { "agent_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_answer_grounding_findings_evaluator_version_tier",
                table: "answer_grounding_findings",
                columns: new[] { "evaluator_version", "tier" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "answer_grounding_daily_counters");

            migrationBuilder.DropTable(
                name: "answer_grounding_findings");
        }
    }
}
