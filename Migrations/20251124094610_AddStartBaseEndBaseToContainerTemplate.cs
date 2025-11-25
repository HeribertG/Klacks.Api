using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddStartBaseEndBaseToContainerTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "end_base",
                table: "container_template",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "start_base",
                table: "container_template",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "end_base",
                table: "container_template");

            migrationBuilder.DropColumn(
                name: "start_base",
                table: "container_template");
        }
    }
}
