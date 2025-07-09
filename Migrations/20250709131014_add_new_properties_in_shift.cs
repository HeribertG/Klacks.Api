using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class add_new_properties_in_shift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_shift_macro_id",
                table: "shift");

            migrationBuilder.AddColumn<string>(
                name: "abbreviation",
                table: "shift",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "briefing_time",
                table: "shift",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<Guid>(
                name: "client_id",
                table: "shift",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "debriefing_time",
                table: "shift",
                type: "time without time zone",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "sum_employees",
                table: "shift",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "shift_id",
                table: "group",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_shift_client_id",
                table: "shift",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_macro_id_client_id_status_from_date_until_date",
                table: "shift",
                columns: new[] { "macro_id", "client_id", "status", "from_date", "until_date" });

            migrationBuilder.CreateIndex(
                name: "ix_group_shift_id",
                table: "group",
                column: "shift_id");

            migrationBuilder.AddForeignKey(
                name: "fk_group_shift_shift_id",
                table: "group",
                column: "shift_id",
                principalTable: "shift",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_shift_client_client_id",
                table: "shift",
                column: "client_id",
                principalTable: "client",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_group_shift_shift_id",
                table: "group");

            migrationBuilder.DropForeignKey(
                name: "fk_shift_client_client_id",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_shift_client_id",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_shift_macro_id_client_id_status_from_date_until_date",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_group_shift_id",
                table: "group");

            migrationBuilder.DropColumn(
                name: "abbreviation",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "briefing_time",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "debriefing_time",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "sum_employees",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "shift_id",
                table: "group");

            migrationBuilder.CreateIndex(
                name: "ix_shift_macro_id",
                table: "shift",
                column: "macro_id");
        }
    }
}
