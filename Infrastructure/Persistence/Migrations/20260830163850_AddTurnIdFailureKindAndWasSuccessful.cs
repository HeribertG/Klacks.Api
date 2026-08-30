using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnIdFailureKindAndWasSuccessful : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "failure_kind",
                table: "skill_usage_records",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "turn_id",
                table: "skill_usage_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "was_successful",
                table: "skill_selection_trajectories",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "failure_kind",
                table: "skill_usage_records");

            migrationBuilder.DropColumn(
                name: "turn_id",
                table: "skill_usage_records");

            migrationBuilder.DropColumn(
                name: "was_successful",
                table: "skill_selection_trajectories");
        }
    }
}
