using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompensatoryRestObligation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "compensatory_rest_obligation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    trigger_date = table.Column<DateOnly>(type: "date", nullable: false),
                    rest_gap_start = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    shortfall_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    standard_rest_hours = table.Column<decimal>(type: "numeric", nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    fulfilled_on = table.Column<DateOnly>(type: "date", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compensatory_rest_obligation", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_compensatory_rest_obligation_client_id_due_date",
                table: "compensatory_rest_obligation",
                columns: new[] { "client_id", "due_date" },
                filter: "is_deleted = false AND fulfilled_on IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_compensatory_rest_obligation_client_id_rest_gap_start",
                table: "compensatory_rest_obligation",
                columns: new[] { "client_id", "rest_gap_start" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "compensatory_rest_obligation");
        }
    }
}
