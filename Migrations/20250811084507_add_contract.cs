using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class add_contract : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "contract_id",
                table: "membership",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "contract",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    guaranteed_hours_per_month = table.Column<decimal>(type: "numeric", nullable: false),
                    maximum_hours_per_month = table.Column<decimal>(type: "numeric", nullable: false),
                    minimum_hours_per_month = table.Column<decimal>(type: "numeric", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    calendar_selection_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_calendar_selection_calendar_selection_id",
                        column: x => x.calendar_selection_id,
                        principalTable: "calendar_selection",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shift_day_assignments",
                columns: table => new
                {
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    shift_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "ix_membership_contract_id",
                table: "membership",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_calendar_selection_id",
                table: "contract",
                column: "calendar_selection_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_name_valid_from_valid_until",
                table: "contract",
                columns: new[] { "name", "valid_from", "valid_until" });

            migrationBuilder.AddForeignKey(
                name: "fk_membership_contract_contract_id",
                table: "membership",
                column: "contract_id",
                principalTable: "contract",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_membership_contract_contract_id",
                table: "membership");

            migrationBuilder.DropTable(
                name: "contract");

            migrationBuilder.DropTable(
                name: "shift_day_assignments");

            migrationBuilder.DropIndex(
                name: "ix_membership_contract_id",
                table: "membership");

            migrationBuilder.DropColumn(
                name: "contract_id",
                table: "membership");
        }
    }
}
