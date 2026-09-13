using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Package B schema: per-item eval persistence plus the golden-case partitions the gate reads.
    /// Existing golden cases default to origin 'cluster' and partition 'holdout', which is factually
    /// correct - every case written so far came from a cluster and none of them was ever training data.
    /// proposed_skill_changes.status is widened from 16 to 32 characters because
    /// ProposedChangeStatuses.BlockedRegression is 18 and every blocked verdict would otherwise fail
    /// with 22001 the first time the gate withheld a proposal.
    /// </summary>
    public partial class KlacksyLearningGateAndEvalItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "skill_learning_golden_cases",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "cluster");

            migrationBuilder.AddColumn<string>(
                name: "partition",
                table: "skill_learning_golden_cases",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "holdout");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "proposed_skill_changes",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "proposed_skill_changes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "correction");

            migrationBuilder.CreateTable(
                name: "eval_run_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    eval_run_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    expected_tool = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    chosen_tool = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    toolset_names_json = table.Column<string>(type: "jsonb", nullable: false),
                    retrieval_hit = table.Column<bool>(type: "boolean", nullable: true),
                    selection_hit = table.Column<bool>(type: "boolean", nullable: true),
                    passed = table.Column<bool>(type: "boolean", nullable: false),
                    latency_ms = table.Column<int>(type: "integer", nullable: false),
                    learning_consumed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_eval_run_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_eval_run_items_eval_runs_eval_run_id",
                        column: x => x.eval_run_id,
                        principalTable: "eval_runs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_golden_cases_origin_partition",
                table: "skill_learning_golden_cases",
                columns: new[] { "origin", "partition" });

            migrationBuilder.CreateIndex(
                name: "ix_eval_run_items_eval_run_id",
                table: "eval_run_items",
                column: "eval_run_id");

            migrationBuilder.CreateIndex(
                name: "ix_eval_run_items_eval_run_id_retrieval_hit_selection_hit_lear",
                table: "eval_run_items",
                columns: new[] { "eval_run_id", "retrieval_hit", "selection_hit", "learning_consumed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "eval_run_items");

            migrationBuilder.DropIndex(
                name: "ix_skill_learning_golden_cases_origin_partition",
                table: "skill_learning_golden_cases");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "skill_learning_golden_cases");

            migrationBuilder.DropColumn(
                name: "partition",
                table: "skill_learning_golden_cases");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "proposed_skill_changes");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "proposed_skill_changes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);
        }
    }
}
