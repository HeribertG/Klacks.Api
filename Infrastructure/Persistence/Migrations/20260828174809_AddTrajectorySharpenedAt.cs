using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrajectorySharpenedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "sharpened_at_utc",
                table: "skill_selection_trajectories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_sharpened_at_utc",
                table: "skill_selection_trajectories",
                column: "sharpened_at_utc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_sharpened_at_utc",
                table: "skill_selection_trajectories");

            migrationBuilder.DropColumn(
                name: "sharpened_at_utc",
                table: "skill_selection_trajectories");
        }
    }
}
