using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingConfirmationPurposeAndSkillPairing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "purpose",
                table: "pending_confirmations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "GateReplay");

            migrationBuilder.AddColumn<string>(
                name: "paired_apply_skill",
                table: "agent_skills",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "purpose",
                table: "pending_confirmations");

            migrationBuilder.DropColumn(
                name: "paired_apply_skill",
                table: "agent_skills");
        }
    }
}
