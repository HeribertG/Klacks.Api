using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_break_break_reason_break_reason_id",
                table: "break");

            migrationBuilder.AlterColumn<Guid>(
                name: "break_reason_id",
                table: "break",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateIndex(
                name: "ix_break_reason_is_deleted_name",
                table: "break_reason",
                columns: new[] { "is_deleted", "name" });

            migrationBuilder.AddForeignKey(
                name: "fk_break_break_reason_break_reason_id",
                table: "break",
                column: "break_reason_id",
                principalTable: "break_reason",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_break_break_reason_break_reason_id",
                table: "break");

            migrationBuilder.DropIndex(
                name: "ix_break_reason_is_deleted_name",
                table: "break_reason");

            migrationBuilder.AlterColumn<Guid>(
                name: "break_reason_id",
                table: "break",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_break_break_reason_break_reason_id",
                table: "break",
                column: "break_reason_id",
                principalTable: "break_reason",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
