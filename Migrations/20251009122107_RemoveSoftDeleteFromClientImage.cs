using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSoftDeleteFromClientImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "current_user_created",
                table: "client_image");

            migrationBuilder.DropColumn(
                name: "current_user_deleted",
                table: "client_image");

            migrationBuilder.DropColumn(
                name: "current_user_updated",
                table: "client_image");

            migrationBuilder.DropColumn(
                name: "deleted_time",
                table: "client_image");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "client_image");

            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "client_image",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "client_image",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "update_time",
                table: "client_image",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "create_time",
                table: "client_image",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "current_user_created",
                table: "client_image",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_user_deleted",
                table: "client_image",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "current_user_updated",
                table: "client_image",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_time",
                table: "client_image",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "client_image",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
