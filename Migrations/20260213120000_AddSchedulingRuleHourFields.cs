using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingRuleHourFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "default_working_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "overtime_threshold",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "guaranteed_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "minimum_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "full_time_hours",
                table: "scheduling_rules",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "default_working_hours", table: "scheduling_rules");
            migrationBuilder.DropColumn(name: "overtime_threshold", table: "scheduling_rules");
            migrationBuilder.DropColumn(name: "guaranteed_hours", table: "scheduling_rules");
            migrationBuilder.DropColumn(name: "maximum_hours", table: "scheduling_rules");
            migrationBuilder.DropColumn(name: "minimum_hours", table: "scheduling_rules");
            migrationBuilder.DropColumn(name: "full_time_hours", table: "scheduling_rules");
        }
    }
}
