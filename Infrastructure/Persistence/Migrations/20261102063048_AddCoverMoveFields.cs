using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCoverMoveFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "moved_from_client_id",
                table: "work",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "kind",
                table: "replacement_requests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "reversed_at_utc",
                table: "replacement_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "work_id",
                table: "replacement_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE replacement_requests AS r
                SET work_id = wc.work_id
                FROM work_change AS wc
                WHERE r.work_change_id = wc.id AND r.work_id IS NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_replacement_requests_work_id",
                table: "replacement_requests",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_replacement_requests_work_id",
                table: "replacement_requests");

            migrationBuilder.DropColumn(
                name: "moved_from_client_id",
                table: "work");

            migrationBuilder.DropColumn(
                name: "kind",
                table: "replacement_requests");

            migrationBuilder.DropColumn(
                name: "reversed_at_utc",
                table: "replacement_requests");

            migrationBuilder.DropColumn(
                name: "work_id",
                table: "replacement_requests");
        }
    }
}
