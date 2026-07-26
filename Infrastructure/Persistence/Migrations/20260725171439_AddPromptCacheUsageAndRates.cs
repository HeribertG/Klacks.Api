using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPromptCacheUsageAndRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cache_creation_input_tokens",
                table: "llm_usages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "cache_read_input_tokens",
                table: "llm_usages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_per_cache_read_token",
                table: "llm_models",
                type: "numeric(10,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "cost_per_cache_write_token",
                table: "llm_models",
                type: "numeric(10,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cache_creation_input_tokens",
                table: "llm_usages");

            migrationBuilder.DropColumn(
                name: "cache_read_input_tokens",
                table: "llm_usages");

            migrationBuilder.DropColumn(
                name: "cost_per_cache_read_token",
                table: "llm_models");

            migrationBuilder.DropColumn(
                name: "cost_per_cache_write_token",
                table: "llm_models");
        }
    }
}
