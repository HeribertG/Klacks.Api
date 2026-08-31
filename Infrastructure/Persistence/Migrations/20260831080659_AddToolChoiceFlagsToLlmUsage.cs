using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddToolChoiceFlagsToLlmUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "tool_call_returned",
                table: "llm_usages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "tool_choice_requested",
                table: "llm_usages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "tool_choice_supported",
                table: "llm_usages",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tool_call_returned",
                table: "llm_usages");

            migrationBuilder.DropColumn(
                name: "tool_choice_requested",
                table: "llm_usages");

            migrationBuilder.DropColumn(
                name: "tool_choice_supported",
                table: "llm_usages");
        }
    }
}
