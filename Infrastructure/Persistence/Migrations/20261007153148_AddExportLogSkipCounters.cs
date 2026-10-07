using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportLogSkipCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "absence_mapping_invalid",
                table: "export_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "skipped_absence_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skipped_superseded_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skipped_unmapped_base_wage_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skipped_unmapped_surcharge_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skipped_unsupported_kind_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "skipped_unsupported_unit_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "absence_mapping_invalid",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "skipped_absence_count",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "skipped_superseded_count",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "skipped_unmapped_base_wage_count",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "skipped_unmapped_surcharge_count",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "skipped_unsupported_kind_count",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "skipped_unsupported_unit_count",
                table: "export_log");
        }
    }
}
