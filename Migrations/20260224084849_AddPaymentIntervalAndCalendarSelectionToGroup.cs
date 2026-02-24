using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentIntervalAndCalendarSelectionToGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "payment_interval",
                table: "group",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "calendar_selection_id",
                table: "group",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_calendar_selection_id",
                table: "group",
                column: "calendar_selection_id");

            migrationBuilder.AddForeignKey(
                name: "fk_group_calendar_selection_calendar_selection_id",
                table: "group",
                column: "calendar_selection_id",
                principalTable: "calendar_selection",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_calendar_selection_calendar_selection_id",
                table: "group");

            migrationBuilder.DropIndex(
                name: "ix_group_calendar_selection_id",
                table: "group");

            migrationBuilder.DropColumn(
                name: "calendar_selection_id",
                table: "group");

            migrationBuilder.DropColumn(
                name: "payment_interval",
                table: "group");
        }
    }
}
