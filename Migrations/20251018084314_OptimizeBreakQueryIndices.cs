using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeBreakQueryIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_membership_client_id_valid_from_valid_until_is_deleted",
                table: "membership",
                columns: new[] { "client_id", "valid_from", "valid_until", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_break_is_deleted_client_id_from_until",
                table: "break",
                columns: new[] { "is_deleted", "client_id", "from", "until" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_membership_client_id_valid_from_valid_until_is_deleted",
                table: "membership");

            migrationBuilder.DropIndex(
                name: "ix_break_is_deleted_client_id_from_until",
                table: "break");
        }
    }
}
