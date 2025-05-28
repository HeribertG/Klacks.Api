using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class addGroupItemlistInClient : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item");

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item");

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id");
        }
    }
}
