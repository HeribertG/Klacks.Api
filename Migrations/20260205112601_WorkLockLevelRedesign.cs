using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class WorkLockLevelRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "day_approvals");

            migrationBuilder.DropTable(
                name: "period_closures");

            migrationBuilder.RenameColumn(
                name: "confirmed_by",
                table: "work",
                newName: "sealed_by");

            migrationBuilder.RenameColumn(
                name: "confirmed_at",
                table: "work",
                newName: "sealed_at");

            migrationBuilder.RenameColumn(
                name: "confirmed_by",
                table: "break",
                newName: "sealed_by");

            migrationBuilder.RenameColumn(
                name: "confirmed_at",
                table: "break",
                newName: "sealed_at");

            migrationBuilder.AddColumn<int>(
                name: "lock_level",
                table: "work",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "lock_level",
                table: "break",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "lock_level",
                table: "work");

            migrationBuilder.DropColumn(
                name: "lock_level",
                table: "break");

            migrationBuilder.RenameColumn(
                name: "sealed_by",
                table: "work",
                newName: "confirmed_by");

            migrationBuilder.RenameColumn(
                name: "sealed_at",
                table: "work",
                newName: "confirmed_at");

            migrationBuilder.RenameColumn(
                name: "sealed_by",
                table: "break",
                newName: "confirmed_by");

            migrationBuilder.RenameColumn(
                name: "sealed_at",
                table: "break",
                newName: "confirmed_at");

            migrationBuilder.CreateTable(
                name: "day_approvals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approval_date = table.Column<DateOnly>(type: "date", nullable: false),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    approved_by = table.Column<string>(type: "text", nullable: false),
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
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    closed_by = table.Column<string>(type: "text", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_period_closures", x => x.id);
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
        }
    }
}
