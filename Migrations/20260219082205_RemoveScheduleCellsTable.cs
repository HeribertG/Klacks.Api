using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveScheduleCellsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "schedule_cells");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "schedule_cells",
                columns: table => new
                {
                    abbreviation = table.Column<string>(type: "text", nullable: true),
                    amount = table.Column<decimal>(type: "numeric", nullable: true),
                    change_time = table.Column<decimal>(type: "numeric", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    end_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    entry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_name = table.Column<string>(type: "text", nullable: true),
                    entry_type = table.Column<int>(type: "integer", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    is_group_restricted = table.Column<bool>(type: "boolean", nullable: false),
                    is_replacement_entry = table.Column<bool>(type: "boolean", nullable: false),
                    lock_level = table.Column<int>(type: "integer", nullable: false),
                    replace_client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "interval", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: true),
                    taxable = table.Column<bool>(type: "boolean", nullable: true),
                    to_invoice = table.Column<bool>(type: "boolean", nullable: true),
                    work_change_type = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                });
        }
    }
}
