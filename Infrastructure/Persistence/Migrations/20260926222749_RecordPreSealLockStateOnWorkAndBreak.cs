using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordPreSealLockStateOnWorkAndBreak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "pre_seal_lock_level",
                table: "work",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pre_seal_sealed_at",
                table: "work",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pre_seal_sealed_by",
                table: "work",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "pre_seal_lock_level",
                table: "break",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "pre_seal_sealed_at",
                table: "break",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pre_seal_sealed_by",
                table: "break",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pre_seal_lock_level",
                table: "work");

            migrationBuilder.DropColumn(
                name: "pre_seal_sealed_at",
                table: "work");

            migrationBuilder.DropColumn(
                name: "pre_seal_sealed_by",
                table: "work");

            migrationBuilder.DropColumn(
                name: "pre_seal_lock_level",
                table: "break");

            migrationBuilder.DropColumn(
                name: "pre_seal_sealed_at",
                table: "break");

            migrationBuilder.DropColumn(
                name: "pre_seal_sealed_by",
                table: "break");
        }
    }
}
