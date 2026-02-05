using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReportTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_sealed",
                table: "work");

            migrationBuilder.DropColumn(
                name: "is_sealed",
                table: "break");

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                table: "work",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "confirmed_by",
                table: "work",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_group_restricted",
                table: "schedule_cells",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "lock_level",
                table: "schedule_cells",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "day_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_date = table.Column<DateOnly>(type: "date", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved_by = table.Column<string>(type: "text", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_day_approvals", x => x.id);
                    table.ForeignKey(
                        name: "fk_day_approvals_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "period_closures",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    closed_by = table.Column<string>(type: "text", nullable: false),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_period_closures", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    page_setup = table.Column<string>(type: "jsonb", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("pk_report_templates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_day_approvals_approval_date_group_id",
                table: "day_approvals",
                columns: new[] { "approval_date", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_day_approvals_group_id",
                table: "day_approvals",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_period_closures_start_date_end_date",
                table: "period_closures",
                columns: new[] { "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "ix_report_templates_is_deleted_type_name",
                table: "report_templates",
                columns: new[] { "is_deleted", "type", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "day_approvals");

            migrationBuilder.DropTable(
                name: "period_closures");

            migrationBuilder.DropTable(
                name: "report_templates");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                table: "work");

            migrationBuilder.DropColumn(
                name: "confirmed_by",
                table: "work");

            migrationBuilder.DropColumn(
                name: "is_group_restricted",
                table: "schedule_cells");

            migrationBuilder.DropColumn(
                name: "lock_level",
                table: "schedule_cells");

            migrationBuilder.AddColumn<bool>(
                name: "is_sealed",
                table: "work",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_sealed",
                table: "break",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
