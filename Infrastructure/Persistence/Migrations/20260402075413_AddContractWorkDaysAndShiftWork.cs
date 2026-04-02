using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractWorkDaysAndShiftWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "performs_shift_work",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_friday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_monday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_saturday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_sunday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_thursday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_tuesday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "work_on_wednesday",
                table: "contract",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                "UPDATE contract SET work_on_monday = true, work_on_tuesday = true, work_on_wednesday = true, work_on_thursday = true, work_on_friday = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "performs_shift_work",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_friday",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_monday",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_saturday",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_sunday",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_thursday",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_tuesday",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "work_on_wednesday",
                table: "contract");
        }
    }
}
