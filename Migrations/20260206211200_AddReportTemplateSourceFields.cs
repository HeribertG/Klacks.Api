using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTemplateSourceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "data_set_ids",
                table: "report_templates",
                type: "jsonb",
                nullable: false,
                defaultValue: "[\"work\"]");

            migrationBuilder.AddColumn<string>(
                name: "source_id",
                table: "report_templates",
                type: "text",
                nullable: false,
                defaultValue: "schedule");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "data_set_ids",
                table: "report_templates");

            migrationBuilder.DropColumn(
                name: "source_id",
                table: "report_templates");
        }
    }
}
