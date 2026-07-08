using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurchargeItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "surcharge_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: true),
                    break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_change_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surcharge_item", x => x.id);
                    table.CheckConstraint("CK_SurchargeItem_ExactlyOneParent", "((CASE WHEN work_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN break_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN work_change_id IS NOT NULL THEN 1 ELSE 0 END)) = 1");
                    table.ForeignKey(
                        name: "fk_surcharge_item_break_break_id",
                        column: x => x.break_id,
                        principalTable: "break",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_surcharge_item_work_change_work_change_id",
                        column: x => x.work_change_id,
                        principalTable: "work_change",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_surcharge_item_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_surcharge_item_break_id",
                table: "surcharge_item",
                column: "break_id");

            migrationBuilder.CreateIndex(
                name: "ix_surcharge_item_work_change_id",
                table: "surcharge_item",
                column: "work_change_id");

            migrationBuilder.CreateIndex(
                name: "ix_surcharge_item_work_id",
                table: "surcharge_item",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "surcharge_item");
        }
    }
}
