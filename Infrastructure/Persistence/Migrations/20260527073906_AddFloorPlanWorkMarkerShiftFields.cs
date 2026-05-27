using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFloorPlanWorkMarkerShiftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "abbreviation",
                table: "floor_plan_work_marker",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "end_time",
                table: "floor_plan_work_marker",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "from_date",
                table: "floor_plan_work_marker",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "shift_id",
                table: "floor_plan_work_marker",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_time",
                table: "floor_plan_work_marker",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "until_date",
                table: "floor_plan_work_marker",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "abbreviation",
                table: "floor_plan_work_marker");

            migrationBuilder.DropColumn(
                name: "end_time",
                table: "floor_plan_work_marker");

            migrationBuilder.DropColumn(
                name: "from_date",
                table: "floor_plan_work_marker");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "floor_plan_work_marker");

            migrationBuilder.DropColumn(
                name: "start_time",
                table: "floor_plan_work_marker");

            migrationBuilder.DropColumn(
                name: "until_date",
                table: "floor_plan_work_marker");
        }
    }
}
