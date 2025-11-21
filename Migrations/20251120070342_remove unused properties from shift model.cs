using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class removeunusedpropertiesfromshiftmodel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "time_range_end_shift",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "time_range_start_shift",
                table: "shift");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "briefing_time",
                table: "container_template_item",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "debriefing_time",
                table: "container_template_item",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "time_range_end_shift",
                table: "container_template_item",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "time_range_start_shift",
                table: "container_template_item",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "travel_time_after",
                table: "container_template_item",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "travel_time_before",
                table: "container_template_item",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "briefing_time",
                table: "container_template_item");

            migrationBuilder.DropColumn(
                name: "debriefing_time",
                table: "container_template_item");

            migrationBuilder.DropColumn(
                name: "time_range_end_shift",
                table: "container_template_item");

            migrationBuilder.DropColumn(
                name: "time_range_start_shift",
                table: "container_template_item");

            migrationBuilder.DropColumn(
                name: "travel_time_after",
                table: "container_template_item");

            migrationBuilder.DropColumn(
                name: "travel_time_before",
                table: "container_template_item");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "time_range_end_shift",
                table: "shift",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "time_range_start_shift",
                table: "shift",
                type: "time without time zone",
                nullable: true);
        }
    }
}
