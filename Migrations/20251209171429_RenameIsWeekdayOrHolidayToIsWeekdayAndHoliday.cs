using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsWeekdayOrHolidayToIsWeekdayAndHoliday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "is_weekday_or_holiday",
                table: "shift",
                newName: "is_weekday_and_holiday");

            migrationBuilder.RenameColumn(
                name: "is_weekday_or_holiday",
                table: "container_template",
                newName: "is_weekday_and_holiday");

            migrationBuilder.RenameIndex(
                name: "ix_container_template_id_container_id_weekday_is_weekday_or_ho",
                table: "container_template",
                newName: "ix_container_template_id_container_id_weekday_is_weekday_and_h");

            migrationBuilder.AddColumn<string>(
                name: "abbreviation",
                table: "shift_day_assignments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "end_shift",
                table: "shift_day_assignments",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "is_sporadic",
                table: "shift_day_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_time_range",
                table: "shift_day_assignments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "shift_type",
                table: "shift_day_assignments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "start_shift",
                table: "shift_day_assignments",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<decimal>(
                name: "work_time",
                table: "shift_day_assignments",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "abbreviation",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "end_shift",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "is_sporadic",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "is_time_range",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "shift_type",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "start_shift",
                table: "shift_day_assignments");

            migrationBuilder.DropColumn(
                name: "work_time",
                table: "shift_day_assignments");

            migrationBuilder.RenameColumn(
                name: "is_weekday_and_holiday",
                table: "shift",
                newName: "is_weekday_or_holiday");

            migrationBuilder.RenameColumn(
                name: "is_weekday_and_holiday",
                table: "container_template",
                newName: "is_weekday_or_holiday");

            migrationBuilder.RenameIndex(
                name: "ix_container_template_id_container_id_weekday_is_weekday_and_h",
                table: "container_template",
                newName: "ix_container_template_id_container_id_weekday_is_weekday_or_ho");
        }
    }
}
