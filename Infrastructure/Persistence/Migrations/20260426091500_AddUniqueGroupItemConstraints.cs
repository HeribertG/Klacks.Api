// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueGroupItemConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM group_item gi
                USING group_item gi2
                WHERE gi.shift_id IS NOT NULL
                  AND gi.shift_id = gi2.shift_id
                  AND gi.group_id = gi2.group_id
                  AND gi.id < gi2.id;

                DELETE FROM group_item gi
                USING group_item gi2
                WHERE gi.client_id IS NOT NULL
                  AND gi.client_id = gi2.client_id
                  AND gi.group_id = gi2.group_id
                  AND gi.id < gi2.id;

                DELETE FROM group_item
                WHERE shift_id IS NULL AND client_id IS NULL;

                UPDATE ""group"" SET root = id
                WHERE parent IS NULL AND root IS NULL AND lft > 0;
            ");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_shift_id_group_id",
                table: "group_item",
                columns: new[] { "shift_id", "group_id" },
                unique: true,
                filter: "\"shift_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.DropForeignKey(
                name: "fk_group_item_shift_shift_id",
                table: "group_item");

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_shift_shift_id",
                table: "group_item",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_item_shift_shift_id",
                table: "group_item");

            migrationBuilder.AddForeignKey(
                name: "fk_group_item_shift_shift_id",
                table: "group_item",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.DropIndex(
                name: "ix_group_item_shift_id_group_id",
                table: "group_item");

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item");
        }
    }
}
