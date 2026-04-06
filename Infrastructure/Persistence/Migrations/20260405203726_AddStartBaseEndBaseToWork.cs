using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStartBaseEndBaseToWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "end_base",
                table: "work",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_base",
                table: "work",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_base",
                table: "work");

            migrationBuilder.DropColumn(
                name: "start_base",
                table: "work");
        }
    }
}
