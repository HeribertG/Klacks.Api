using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RestrictAbsenceDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_absence_detail_absence_absence_id",
                table: "absence_detail");

            migrationBuilder.DropForeignKey(
                name: "fk_break_absence_absence_id",
                table: "break");

            migrationBuilder.DropForeignKey(
                name: "fk_break_placeholder_absence_absence_id",
                table: "break_placeholder");

            migrationBuilder.AddForeignKey(
                name: "fk_absence_detail_absence_absence_id",
                table: "absence_detail",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_break_absence_absence_id",
                table: "break",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_break_placeholder_absence_absence_id",
                table: "break_placeholder",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_absence_detail_absence_absence_id",
                table: "absence_detail");

            migrationBuilder.DropForeignKey(
                name: "fk_break_absence_absence_id",
                table: "break");

            migrationBuilder.DropForeignKey(
                name: "fk_break_placeholder_absence_absence_id",
                table: "break_placeholder");

            migrationBuilder.AddForeignKey(
                name: "fk_absence_detail_absence_absence_id",
                table: "absence_detail",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_break_absence_absence_id",
                table: "break",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_break_placeholder_absence_absence_id",
                table: "break_placeholder",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
