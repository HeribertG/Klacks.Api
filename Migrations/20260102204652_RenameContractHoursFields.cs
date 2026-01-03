using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameContractHoursFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "guaranteed_hours_per_month",
                table: "contract",
                newName: "guaranteed_hours");

            migrationBuilder.RenameColumn(
                name: "maximum_hours_per_month",
                table: "contract",
                newName: "maximum_hours");

            migrationBuilder.RenameColumn(
                name: "minimum_hours_per_month",
                table: "contract",
                newName: "minimum_hours");

            migrationBuilder.AddColumn<decimal>(
                name: "full_time",
                table: "contract",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "night_rate",
                table: "contract",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "holiday_rate",
                table: "contract",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "weekend_rate",
                table: "contract",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "payment_interval",
                table: "contract",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "full_time",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "night_rate",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "holiday_rate",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "weekend_rate",
                table: "contract");

            migrationBuilder.DropColumn(
                name: "payment_interval",
                table: "contract");

            migrationBuilder.RenameColumn(
                name: "guaranteed_hours",
                table: "contract",
                newName: "guaranteed_hours_per_month");

            migrationBuilder.RenameColumn(
                name: "maximum_hours",
                table: "contract",
                newName: "maximum_hours_per_month");

            migrationBuilder.RenameColumn(
                name: "minimum_hours",
                table: "contract",
                newName: "minimum_hours_per_month");
        }
    }
}
