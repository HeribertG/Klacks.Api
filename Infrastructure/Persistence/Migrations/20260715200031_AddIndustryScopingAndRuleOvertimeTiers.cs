using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIndustryScopingAndRuleOvertimeTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "overtime_basis",
                table: "scheduling_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "overtime_rate_mode",
                table: "scheduling_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier1after_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier1rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier2after_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier2rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier3after_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_tier3rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scheduling_rule_id",
                table: "rest_day_rotation_rule",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scheduling_rule_id",
                table: "period_cap_rule",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "overtime_basis",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_rate_mode",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_tier1after_hours",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_tier1rate",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_tier2after_hours",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_tier2rate",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_tier3after_hours",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "overtime_tier3rate",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "scheduling_rule_id",
                table: "rest_day_rotation_rule");

            migrationBuilder.DropColumn(
                name: "scheduling_rule_id",
                table: "period_cap_rule");
        }
    }
}
