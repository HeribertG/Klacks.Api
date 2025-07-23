using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class add_lft_rgt_in_shift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "lft",
                table: "shift",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rgt",
                table: "shift",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lft",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "rgt",
                table: "shift");
        }
    }
}
