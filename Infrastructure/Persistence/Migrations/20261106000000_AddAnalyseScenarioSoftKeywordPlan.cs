using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds analyse_scenarios.soft_keyword_plan: true when the run that produced the scenario planned with the schedule
    /// keywords as wishes (Soft keyword mode); such a scenario is never accepted automatically. Existing scenarios were
    /// all planned with the keywords as vetoes (false).
    /// </summary>
    public partial class AddAnalyseScenarioSoftKeywordPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "soft_keyword_plan",
                table: "analyse_scenarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "soft_keyword_plan",
                table: "analyse_scenarios");
        }
    }
}
