using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddParentWorkIdToWorkAndBreak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "parent_work_id",
                table: "work",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "parent_work_id",
                table: "break",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_parent_work_id",
                table: "work",
                column: "parent_work_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_parent_work_id",
                table: "break",
                column: "parent_work_id");

            migrationBuilder.AddForeignKey(
                name: "fk_break_work_parent_work_id",
                table: "break",
                column: "parent_work_id",
                principalTable: "work",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_work_work_parent_work_id",
                table: "work",
                column: "parent_work_id",
                principalTable: "work",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_break_work_parent_work_id",
                table: "break");

            migrationBuilder.DropForeignKey(
                name: "fk_work_work_parent_work_id",
                table: "work");

            migrationBuilder.DropIndex(
                name: "ix_work_parent_work_id",
                table: "work");

            migrationBuilder.DropIndex(
                name: "ix_break_parent_work_id",
                table: "break");

            migrationBuilder.DropColumn(
                name: "parent_work_id",
                table: "work");

            migrationBuilder.DropColumn(
                name: "parent_work_id",
                table: "break");
        }
    }
}
