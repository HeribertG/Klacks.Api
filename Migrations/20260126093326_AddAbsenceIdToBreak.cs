using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAbsenceIdToBreak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "absence_id",
                table: "break",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_break_absence_id",
                table: "break",
                column: "absence_id");

            migrationBuilder.AddForeignKey(
                name: "fk_break_absence_absence_id",
                table: "break",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_break_absence_absence_id",
                table: "break");

            migrationBuilder.DropIndex(
                name: "ix_break_absence_id",
                table: "break");

            migrationBuilder.DropColumn(
                name: "absence_id",
                table: "break");
        }
    }
}
