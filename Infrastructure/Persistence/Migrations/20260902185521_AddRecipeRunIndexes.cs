using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeRunIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "abort_reason",
                table: "recipe_runs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_recipe_runs_conversation_id_status",
                table: "recipe_runs",
                columns: new[] { "conversation_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_recipe_runs_update_time",
                table: "recipe_runs",
                column: "update_time");

            migrationBuilder.CreateIndex(
                name: "ix_recipe_runs_user_id",
                table: "recipe_runs",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_recipe_runs_conversation_id_status",
                table: "recipe_runs");

            migrationBuilder.DropIndex(
                name: "ix_recipe_runs_update_time",
                table: "recipe_runs");

            migrationBuilder.DropIndex(
                name: "ix_recipe_runs_user_id",
                table: "recipe_runs");

            migrationBuilder.AlterColumn<string>(
                name: "abort_reason",
                table: "recipe_runs",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
