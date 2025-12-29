using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameShiftTimesToStartEndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_schedule_entries");

            migrationBuilder.RenameColumn(
                name: "start_shift",
                table: "work",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "end_shift",
                table: "work",
                newName: "end_time");

            migrationBuilder.RenameColumn(
                name: "start_shift",
                table: "break",
                newName: "start_time");

            migrationBuilder.RenameColumn(
                name: "end_shift",
                table: "break",
                newName: "end_time");

            migrationBuilder.CreateTable(
                name: "schedule_cells",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    change_time = table.Column<decimal>(type: "numeric", nullable: true),
                    work_change_type = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    to_invoice = table.Column<bool>(type: "boolean", nullable: true),
                    taxable = table.Column<bool>(type: "boolean", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_name = table.Column<string>(type: "text", nullable: true),
                    abbreviation = table.Column<string>(type: "text", nullable: true),
                    replace_client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_replacement_entry = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_cells");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "work",
                newName: "start_shift");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "work",
                newName: "end_shift");

            migrationBuilder.RenameColumn(
                name: "start_time",
                table: "break",
                newName: "start_shift");

            migrationBuilder.RenameColumn(
                name: "end_time",
                table: "break",
                newName: "end_shift");

            migrationBuilder.CreateTable(
                name: "work_schedule_entries",
                columns: table => new
                {
                    abbreviation = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    change_time = table.Column<decimal>(type: "numeric", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    end_shift = table.Column<TimeSpan>(type: "interval", nullable: false),
                    entry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_replacement_entry = table.Column<bool>(type: "boolean", nullable: false),
                    replace_client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_name = table.Column<string>(type: "text", nullable: true),
                    start_shift = table.Column<TimeSpan>(type: "interval", nullable: false),
                    taxable = table.Column<bool>(type: "boolean", nullable: true),
                    to_invoice = table.Column<bool>(type: "boolean", nullable: true),
                    work_change_type = table.Column<int>(type: "integer", nullable: true),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                });
        }
    }
}
