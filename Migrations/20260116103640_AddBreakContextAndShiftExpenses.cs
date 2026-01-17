using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBreakContextAndShiftExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_break_break_reason_break_reason_id",
                table: "break");

            migrationBuilder.DropForeignKey(
                name: "fk_break_placeholder_break_reason_break_reason_id",
                table: "break_placeholder");

            migrationBuilder.DropTable(
                name: "break_reason");

            migrationBuilder.DropIndex(
                name: "ix_break_placeholder_break_reason_id",
                table: "break_placeholder");

            migrationBuilder.DropIndex(
                name: "ix_break_break_reason_id",
                table: "break");

            migrationBuilder.DropIndex(
                name: "ix_break_client_id_break_reason_id",
                table: "break");

            migrationBuilder.DropColumn(
                name: "break_reason_id",
                table: "break_placeholder");

            migrationBuilder.DropColumn(
                name: "break_reason_id",
                table: "break");

            migrationBuilder.CreateTable(
                name: "break_context",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_break = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_break = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    detail_name_de = table.Column<string>(type: "text", nullable: true),
                    detail_name_en = table.Column<string>(type: "text", nullable: true),
                    detail_name_fr = table.Column<string>(type: "text", nullable: true),
                    detail_name_it = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_break_context", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_context_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    taxable = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_shift_expenses", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_expenses_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_break_client_id",
                table: "break",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_context_absence_id",
                table: "break_context",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_context_is_deleted_absence_id",
                table: "break_context",
                columns: new[] { "is_deleted", "absence_id" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_expenses_shift_id",
                table: "shift_expenses",
                column: "shift_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "break_context");

            migrationBuilder.DropTable(
                name: "shift_expenses");

            migrationBuilder.DropIndex(
                name: "ix_break_client_id",
                table: "break");

            migrationBuilder.AddColumn<Guid>(
                name: "break_reason_id",
                table: "break_placeholder",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "break_reason_id",
                table: "break",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "break_reason",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    default_length = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<double>(type: "double precision", nullable: false),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    hide_in_gantt = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    macro = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    undeletable = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break_reason", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_break_placeholder_break_reason_id",
                table: "break_placeholder",
                column: "break_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_break_reason_id",
                table: "break",
                column: "break_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_client_id_break_reason_id",
                table: "break",
                columns: new[] { "client_id", "break_reason_id" });

            migrationBuilder.CreateIndex(
                name: "ix_break_reason_is_deleted_name",
                table: "break_reason",
                columns: new[] { "is_deleted", "name" });

            migrationBuilder.AddForeignKey(
                name: "fk_break_break_reason_break_reason_id",
                table: "break",
                column: "break_reason_id",
                principalTable: "break_reason",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_break_placeholder_break_reason_break_reason_id",
                table: "break_placeholder",
                column: "break_reason_id",
                principalTable: "break_reason",
                principalColumn: "id");
        }
    }
}
