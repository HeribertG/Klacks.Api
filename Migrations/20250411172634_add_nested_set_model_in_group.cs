using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class add_nested_set_model_in_group : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_selected_calendar_state_country",
                table: "selected_calendar");

            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateSequence<int>(
                name: "client_idnumber_seq",
                schema: "public",
                startValue: 203L,
                incrementBy: 1);

            migrationBuilder.AddColumn<int>(
                name: "lft",
                table: "group",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "parend",
                table: "group",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rgt",
                table: "group",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "root",
                table: "group",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "id_number",
                table: "client",
                type: "integer",
                nullable: false,
                defaultValueSql: "nextval('public.client_idnumber_seq')",
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_state_country_calendar_selection_id",
                table: "selected_calendar",
                columns: new[] { "state", "country", "calendar_selection_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_selected_calendar_state_country_calendar_selection_id",
                table: "selected_calendar");

            migrationBuilder.DropColumn(
                name: "lft",
                table: "group");

            migrationBuilder.DropColumn(
                name: "parend",
                table: "group");

            migrationBuilder.DropColumn(
                name: "rgt",
                table: "group");

            migrationBuilder.DropColumn(
                name: "root",
                table: "group");

            migrationBuilder.DropSequence(
                name: "client_idnumber_seq",
                schema: "public");

            migrationBuilder.AlterColumn<int>(
                name: "id_number",
                table: "client",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValueSql: "nextval('public.client_idnumber_seq')");

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_state_country",
                table: "selected_calendar",
                columns: new[] { "state", "country" });
        }
    }
}
