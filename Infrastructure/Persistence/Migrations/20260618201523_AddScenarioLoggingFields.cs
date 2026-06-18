using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioLoggingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "churn_ratio",
                table: "analyse_scenarios",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reject_reason",
                table: "analyse_scenarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "reject_reason_text",
                table: "analyse_scenarios",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "stage0violations",
                table: "analyse_scenarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sub_score_json",
                table: "analyse_scenarios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "churn_ratio",
                table: "analyse_scenarios");

            migrationBuilder.DropColumn(
                name: "reject_reason",
                table: "analyse_scenarios");

            migrationBuilder.DropColumn(
                name: "reject_reason_text",
                table: "analyse_scenarios");

            migrationBuilder.DropColumn(
                name: "stage0violations",
                table: "analyse_scenarios");

            migrationBuilder.DropColumn(
                name: "sub_score_json",
                table: "analyse_scenarios");
        }
    }
}
