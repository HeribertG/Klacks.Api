using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDayApprovalRowsAndPreSealOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sealed_day_date_global",
                table: "sealed_day");

            migrationBuilder.DropIndex(
                name: "ix_sealed_day_date_group",
                table: "sealed_day");

            migrationBuilder.AddColumn<Guid>(
                name: "pre_seal_sealed_by_group_id",
                table: "break",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_global",
                table: "sealed_day",
                columns: new[] { "date", "level" },
                unique: true,
                filter: "\"group_id\" IS NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_group",
                table: "sealed_day",
                columns: new[] { "date", "group_id", "level" },
                unique: true,
                filter: "\"group_id\" IS NOT NULL AND \"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM sealed_day WHERE level <> 3;");

            migrationBuilder.DropIndex(
                name: "ix_sealed_day_date_global",
                table: "sealed_day");

            migrationBuilder.DropIndex(
                name: "ix_sealed_day_date_group",
                table: "sealed_day");

            migrationBuilder.DropColumn(
                name: "pre_seal_sealed_by_group_id",
                table: "break");

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_global",
                table: "sealed_day",
                column: "date",
                unique: true,
                filter: "\"group_id\" IS NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_group",
                table: "sealed_day",
                columns: new[] { "date", "group_id" },
                unique: true,
                filter: "\"group_id\" IS NOT NULL AND \"is_deleted\" = false");
        }
    }
}
