using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScenarioSourceShiftIdToShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "scenario_source_shift_id",
                table: "shift",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "source_child_count_snapshot",
                table: "shift",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_scenario_source_shift_id",
                table: "shift",
                column: "scenario_source_shift_id",
                filter: "scenario_source_shift_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_shift_scenario_source_shift_id",
                table: "shift",
                column: "scenario_source_shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_shift_scenario_source_shift_id",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_shift_scenario_source_shift_id",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "scenario_source_shift_id",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "source_child_count_snapshot",
                table: "shift");
        }
    }
}
