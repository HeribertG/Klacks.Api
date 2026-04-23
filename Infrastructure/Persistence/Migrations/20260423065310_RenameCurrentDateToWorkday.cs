using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameCurrentDateToWorkday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "current_date",
                table: "work",
                newName: "workday");

            migrationBuilder.RenameColumn(
                name: "current_date",
                table: "schedule_notes",
                newName: "workday");

            migrationBuilder.RenameColumn(
                name: "current_date",
                table: "schedule_commands",
                newName: "workday");

            migrationBuilder.RenameColumn(
                name: "current_date",
                table: "break",
                newName: "workday");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "workday",
                table: "work",
                newName: "current_date");

            migrationBuilder.RenameColumn(
                name: "workday",
                table: "schedule_notes",
                newName: "current_date");

            migrationBuilder.RenameColumn(
                name: "workday",
                table: "schedule_commands",
                newName: "current_date");

            migrationBuilder.RenameColumn(
                name: "workday",
                table: "break",
                newName: "current_date");
        }
    }
}
