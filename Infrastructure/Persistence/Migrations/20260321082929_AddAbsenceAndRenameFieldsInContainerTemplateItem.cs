using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAbsenceAndRenameFieldsInContainerTemplateItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_container_template_item_shift_shift_id",
                table: "container_template_item");

            migrationBuilder.RenameColumn(
                name: "time_range_start_shift",
                table: "container_template_item",
                newName: "time_range_start_item");

            migrationBuilder.RenameColumn(
                name: "time_range_end_shift",
                table: "container_template_item",
                newName: "time_range_end_item");

            migrationBuilder.RenameColumn(
                name: "start_shift",
                table: "container_template_item",
                newName: "start_item");

            migrationBuilder.RenameColumn(
                name: "end_shift",
                table: "container_template_item",
                newName: "end_item");

            migrationBuilder.AlterColumn<Guid>(
                name: "shift_id",
                table: "container_template_item",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "absence_id",
                table: "container_template_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_container_template_item_absence_id",
                table: "container_template_item",
                column: "absence_id");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ContainerTemplateItem_ShiftXorAbsence",
                table: "container_template_item",
                sql: "(shift_id IS NOT NULL AND absence_id IS NULL) OR (shift_id IS NULL AND absence_id IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "fk_container_template_item_absence_absence_id",
                table: "container_template_item",
                column: "absence_id",
                principalTable: "absence",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_container_template_item_shift_shift_id",
                table: "container_template_item",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_container_template_item_absence_absence_id",
                table: "container_template_item");

            migrationBuilder.DropForeignKey(
                name: "fk_container_template_item_shift_shift_id",
                table: "container_template_item");

            migrationBuilder.DropIndex(
                name: "ix_container_template_item_absence_id",
                table: "container_template_item");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ContainerTemplateItem_ShiftXorAbsence",
                table: "container_template_item");

            migrationBuilder.DropColumn(
                name: "absence_id",
                table: "container_template_item");

            migrationBuilder.RenameColumn(
                name: "time_range_start_item",
                table: "container_template_item",
                newName: "time_range_start_shift");

            migrationBuilder.RenameColumn(
                name: "time_range_end_item",
                table: "container_template_item",
                newName: "time_range_end_shift");

            migrationBuilder.RenameColumn(
                name: "start_item",
                table: "container_template_item",
                newName: "start_shift");

            migrationBuilder.RenameColumn(
                name: "end_item",
                table: "container_template_item",
                newName: "end_shift");

            migrationBuilder.AlterColumn<Guid>(
                name: "shift_id",
                table: "container_template_item",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_container_template_item_shift_shift_id",
                table: "container_template_item",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
