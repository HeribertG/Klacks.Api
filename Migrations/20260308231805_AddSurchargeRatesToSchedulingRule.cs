using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSurchargeRatesToSchedulingRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "holiday_rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "night_rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sa_rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "so_rate",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "holiday_rate",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "night_rate",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "sa_rate",
                table: "scheduling_rules");

            migrationBuilder.DropColumn(
                name: "so_rate",
                table: "scheduling_rules");
        }
    }
}
