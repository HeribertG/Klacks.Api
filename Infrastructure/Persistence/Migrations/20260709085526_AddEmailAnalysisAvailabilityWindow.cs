using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailAnalysisAvailabilityWindow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "end_hour",
                table: "email_analyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "start_hour",
                table: "email_analyses",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "weekdays",
                table: "email_analyses",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_hour",
                table: "email_analyses");

            migrationBuilder.DropColumn(
                name: "start_hour",
                table: "email_analyses");

            migrationBuilder.DropColumn(
                name: "weekdays",
                table: "email_analyses");
        }
    }
}
