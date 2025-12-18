using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameTaxabledToTaxable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "taxabled",
                table: "expenses",
                newName: "taxable");

            migrationBuilder.CreateTable(
                name: "work_schedule_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    start_shift = table.Column<TimeSpan>(type: "interval", nullable: false),
                    end_shift = table.Column<TimeSpan>(type: "interval", nullable: false),
                    change_time = table.Column<decimal>(type: "numeric", nullable: true),
                    work_change_type = table.Column<int>(type: "integer", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    to_invoice = table.Column<bool>(type: "boolean", nullable: true),
                    taxable = table.Column<bool>(type: "boolean", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_name = table.Column<string>(type: "text", nullable: true),
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
                name: "work_schedule_entries");

            migrationBuilder.RenameColumn(
                name: "taxable",
                table: "expenses",
                newName: "taxabled");
        }
    }
}
