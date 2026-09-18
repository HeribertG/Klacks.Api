using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReachedHitToEvalRunItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "chosen_args_json",
                table: "eval_run_items",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "reached_hit",
                table: "eval_run_items",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "response_text",
                table: "eval_run_items",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tool_sequence_json",
                table: "eval_run_items",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "chosen_args_json",
                table: "eval_run_items");

            migrationBuilder.DropColumn(
                name: "reached_hit",
                table: "eval_run_items");

            migrationBuilder.DropColumn(
                name: "response_text",
                table: "eval_run_items");

            migrationBuilder.DropColumn(
                name: "tool_sequence_json",
                table: "eval_run_items");
        }
    }
}
