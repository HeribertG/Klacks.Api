using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    public partial class RenameBreakContextToAbsenceDetail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_break_context_absence_id",
                table: "break_context");

            migrationBuilder.DropIndex(
                name: "ix_break_context_is_deleted_absence_id",
                table: "break_context");

            migrationBuilder.RenameTable(
                name: "break_context",
                newName: "absence_detail");

            migrationBuilder.RenameColumn(
                name: "start_break",
                table: "absence_detail",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "end_break",
                table: "absence_detail",
                newName: "end_time");

            migrationBuilder.AddColumn<int>(
                name: "mode",
                table: "absence_detail",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "duration",
                table: "absence_detail",
                type: "interval",
                nullable: false,
                defaultValue: TimeSpan.Zero);

            migrationBuilder.Sql("ALTER TABLE absence_detail RENAME CONSTRAINT pk_break_context TO pk_absence_detail;");
            migrationBuilder.Sql("ALTER TABLE absence_detail RENAME CONSTRAINT fk_break_context_absence_absence_id TO fk_absence_detail_absence_absence_id;");

            migrationBuilder.CreateIndex(
                name: "ix_absence_detail_absence_id",
                table: "absence_detail",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_absence_detail_is_deleted_absence_id",
                table: "absence_detail",
                columns: new[] { "is_deleted", "absence_id" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_absence_detail_absence_id",
                table: "absence_detail");

            migrationBuilder.DropIndex(
                name: "ix_absence_detail_is_deleted_absence_id",
                table: "absence_detail");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "absence_detail");

            migrationBuilder.DropColumn(
                name: "duration",
                table: "absence_detail");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "absence_detail",
                newName: "start_break");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "absence_detail",
                newName: "end_break");

            migrationBuilder.Sql("ALTER TABLE absence_detail RENAME CONSTRAINT pk_absence_detail TO pk_break_context;");
            migrationBuilder.Sql("ALTER TABLE absence_detail RENAME CONSTRAINT fk_absence_detail_absence_absence_id TO fk_break_context_absence_absence_id;");

            migrationBuilder.RenameTable(
                name: "absence_detail",
                newName: "break_context");

            migrationBuilder.CreateIndex(
                name: "ix_break_context_absence_id",
                table: "break_context",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_context_is_deleted_absence_id",
                table: "break_context",
                columns: new[] { "is_deleted", "absence_id" });
        }
    }
}
