using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingRuleWorkDaysAndShiftWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "performs_shift_work",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_friday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_monday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_saturday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_sunday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_thursday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_tuesday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_wednesday",
                table: "scheduling_rules",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "performs_shift_work",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_friday",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_monday",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_saturday",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_sunday",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_thursday",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_tuesday",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "work_on_wednesday",
                table: "scheduling_rules");
        }
    }
}
