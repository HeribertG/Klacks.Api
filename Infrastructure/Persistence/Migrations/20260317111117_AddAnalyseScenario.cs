using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyseScenario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "work",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "shift",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "schedule_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "break",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "analyse_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: false),
                    token = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_analyse_scenarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_analyse_scenarios_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_analyse_token",
                table: "work",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_analyse_token",
                table: "shift",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_notes_analyse_token",
                table: "schedule_notes",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_break_analyse_token",
                table: "break",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenarios_group_id_status",
                table: "analyse_scenarios",
                columns: new[] { "group_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenarios_token",
                table: "analyse_scenarios",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analyse_scenarios");

            migrationBuilder.DropIndex(
                name: "ix_work_analyse_token",
                table: "work");

            migrationBuilder.DropIndex(
                name: "ix_shift_analyse_token",
                table: "shift");

            migrationBuilder.DropIndex(
                name: "ix_schedule_notes_analyse_token",
                table: "schedule_notes");

            migrationBuilder.DropIndex(
                name: "ix_break_analyse_token",
                table: "break");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "work");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "shift");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "schedule_notes");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "break");
        }
    }
}
