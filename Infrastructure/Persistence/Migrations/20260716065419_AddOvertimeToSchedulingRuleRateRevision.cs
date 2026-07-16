using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimeToSchedulingRuleRateRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "overtime_basis",
                table: "scheduling_rule_rate_revisions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "overtime_rate_mode",
                table: "scheduling_rule_rate_revisions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier1after_hours",
                table: "scheduling_rule_rate_revisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier1rate",
                table: "scheduling_rule_rate_revisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier2after_hours",
                table: "scheduling_rule_rate_revisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier2rate",
                table: "scheduling_rule_rate_revisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier3after_hours",
                table: "scheduling_rule_rate_revisions",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier3rate",
                table: "scheduling_rule_rate_revisions",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "overtime_basis",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_rate_mode",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_tier1after_hours",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_tier1rate",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_tier2after_hours",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_tier2rate",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_tier3after_hours",
                table: "scheduling_rule_rate_revisions");

            migrationBuilder.DropColumn(
                name: "overtime_tier3rate",
                table: "scheduling_rule_rate_revisions");
        }
    }
}
