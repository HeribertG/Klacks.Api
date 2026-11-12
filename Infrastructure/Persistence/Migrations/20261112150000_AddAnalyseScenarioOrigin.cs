using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds analyse_scenarios.origin: 1 (AbsenceCover) when the cover of an absence produced the scenario, 0 (Other) for
    /// every other producer. A stale cover built on the real plan cannot be accepted with the stale override; it has to be
    /// covered again. Existing scenarios get 0, so they keep today's overridable stale warning.
    /// </summary>
    public partial class AddAnalyseScenarioOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "origin",
                table: "analyse_scenarios",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "origin",
                table: "analyse_scenarios");
        }
    }
}
