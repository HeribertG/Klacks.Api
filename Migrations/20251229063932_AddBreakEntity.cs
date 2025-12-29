using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "break",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    break_reason_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    is_sealed = table.Column<bool>(type: "boolean", nullable: false),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    start_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_break_reason_break_reason_id",
                        column: x => x.break_reason_id,
                        principalTable: "break_reason",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_break_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_break_break_reason_id",
                table: "break",
                column: "break_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_client_id_break_reason_id",
                table: "break",
                columns: new[] { "client_id", "break_reason_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "break");
        }
    }
}
