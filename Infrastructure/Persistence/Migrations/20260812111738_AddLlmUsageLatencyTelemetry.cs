using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmUsageLatencyTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "toolset_assembly_ms",
                table: "llm_usages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "tool_iterations",
                table: "llm_usages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ttft_ms",
                table: "llm_usages",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "toolset_assembly_ms",
                table: "llm_usages");

            migrationBuilder.DropColumn(
                name: "tool_iterations",
                table: "llm_usages");

            migrationBuilder.DropColumn(
                name: "ttft_ms",
                table: "llm_usages");
        }
    }
}
