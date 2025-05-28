using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class addUserIntoGroupItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item");

            migrationBuilder.AlterColumn<Guid>(
                name: "client_id",
                table: "group_item",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "user_id",
                table: "group_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "group_item");

            migrationBuilder.AlterColumn<Guid>(
                name: "client_id",
                table: "group_item",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_client_client_id",
                table: "group_item",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
