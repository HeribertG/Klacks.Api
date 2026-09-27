using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillSeedVersionAndGateMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "gate_metrics_json",
                table: "proposed_skill_changes",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "seed_version",
                table: "agent_skills",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "gate_metrics_json",
                table: "proposed_skill_changes");

            migrationBuilder.DropColumn(
                name: "seed_version",
                table: "agent_skills");
        }
    }
}
