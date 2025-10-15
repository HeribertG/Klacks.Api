using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class CreatePerformanceIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_group_item_group_id",
                table: "group_item");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_group_id_client_id_is_deleted",
                table: "group_item",
                columns: new[] { "group_id", "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_company_name",
                table: "client",
                columns: new[] { "is_deleted", "company", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_first_name_name",
                table: "client",
                columns: new[] { "is_deleted", "first_name", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_id_number",
                table: "client",
                columns: new[] { "is_deleted", "id_number" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_name_first_name",
                table: "client",
                columns: new[] { "is_deleted", "name", "first_name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_group_item_group_id_client_id_is_deleted",
                table: "group_item");

            migrationBuilder.DropIndex(
                name: "ix_client_is_deleted_company_name",
                table: "client");

            migrationBuilder.DropIndex(
                name: "ix_client_is_deleted_first_name_name",
                table: "client");

            migrationBuilder.DropIndex(
                name: "ix_client_is_deleted_id_number",
                table: "client");

            migrationBuilder.DropIndex(
                name: "ix_client_is_deleted_name_first_name",
                table: "client");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_group_id",
                table: "group_item",
                column: "group_id");
        }
    }
}
