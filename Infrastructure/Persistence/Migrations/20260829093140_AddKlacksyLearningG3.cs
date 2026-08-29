using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKlacksyLearningG3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "helpful",
                table: "skill_selection_trajectories",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "learned_phrase_hit",
                table: "skill_selection_trajectories",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recipe_name",
                table: "skill_selection_trajectories",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "agent_recipes",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Seed");

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_learned_phrase_hit_create_time",
                table: "skill_selection_trajectories",
                columns: new[] { "learned_phrase_hit", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_recipe_name_create_time",
                table: "skill_selection_trajectories",
                columns: new[] { "recipe_name", "create_time" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_learned_phrase_hit_create_time",
                table: "skill_selection_trajectories");

            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_recipe_name_create_time",
                table: "skill_selection_trajectories");

            migrationBuilder.DropColumn(
                name: "helpful",
                table: "skill_selection_trajectories");

            migrationBuilder.DropColumn(
                name: "learned_phrase_hit",
                table: "skill_selection_trajectories");

            migrationBuilder.DropColumn(
                name: "recipe_name",
                table: "skill_selection_trajectories");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "agent_recipes");
        }
    }
}
