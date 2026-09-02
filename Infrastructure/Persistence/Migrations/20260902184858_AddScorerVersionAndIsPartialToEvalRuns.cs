using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds scorer_version and is_partial to eval_runs plus the index the baseline lookup uses.
    /// Existing rows are deliberately NOT backfilled: every historical run was scored under the old
    /// rules, so the default scorer_version 1 is factually correct and keeps them out of every
    /// version 2 baseline. is_partial cannot be reconstructed in SQL either - whether a run covered
    /// its whole goldset depends on the goldset file size at the time, which the table never stored -
    /// so historical rows keep the default false and are excluded from baselines by their version.
    /// </summary>
    public partial class AddScorerVersionAndIsPartialToEvalRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_partial",
                table: "eval_runs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "scorer_version",
                table: "eval_runs",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "ix_eval_runs_goldset_model_scorer_version_items_total",
                table: "eval_runs",
                columns: new[] { "goldset", "model", "scorer_version", "items_total" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_eval_runs_goldset_model_scorer_version_items_total",
                table: "eval_runs");

            migrationBuilder.DropColumn(
                name: "is_partial",
                table: "eval_runs");

            migrationBuilder.DropColumn(
                name: "scorer_version",
                table: "eval_runs");
        }
    }
}
