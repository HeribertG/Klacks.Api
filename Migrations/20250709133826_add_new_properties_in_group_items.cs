using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class add_new_properties_in_group_items : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_shift_client_client_id",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item");

            migrationBuilder.AddColumn<Guid>(
                name: "shift_id",
                table: "group_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id_shift_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "shift_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_item_shift_id",
                table: "group_item",
                column: "shift_id");

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_shift_shift_id",
                table: "group_item",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_shift_client_client_id",
                table: "shift",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_item_shift_shift_id",
                table: "group_item");

            migrationBuilder.DropForeignKey(
                name: "fk_shift_client_client_id",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id_shift_id",
                table: "group_item");

            migrationBuilder.DropIndex(
                name: "ix_group_item_shift_id",
                table: "group_item");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "group_item");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_shift_client_client_id",
                table: "shift",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id");
        }
    }
}
