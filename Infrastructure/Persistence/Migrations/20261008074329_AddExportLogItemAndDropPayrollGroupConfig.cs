using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportLogItemAndDropPayrollGroupConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_export_group_config");

            migrationBuilder.AddColumn<bool>(
                name: "is_supplementary",
                table: "export_log",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "person_count",
                table: "export_log",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "storage_key",
                table: "export_log",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "export_log_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    export_log_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    format = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", maxLength: 64, nullable: false),
                    entry_count = table.Column<int>(type: "integer", nullable: false),
                    entries_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_supplementary = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_export_log_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_export_log_item_export_log_export_log_id",
                        column: x => x.export_log_id,
                        principalTable: "export_log",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_export_log_item_client_id_start_date_end_date_format_revisi",
                table: "export_log_item",
                columns: new[] { "client_id", "start_date", "end_date", "format", "revision" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_export_log_item_export_log_id",
                table: "export_log_item",
                column: "export_log_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "export_log_item");

            migrationBuilder.DropColumn(
                name: "is_supplementary",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "person_count",
                table: "export_log");

            migrationBuilder.DropColumn(
                name: "storage_key",
                table: "export_log");

            migrationBuilder.CreateTable(
                name: "payroll_export_group_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_mapping_json = table.Column<string>(type: "text", nullable: false),
                    base_wage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delimiter = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    encoding = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    surcharge_wage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    target_system = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payroll_export_group_config", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_export_group_config_group_id",
                table: "payroll_export_group_config",
                column: "group_id",
                unique: true,
                filter: "\"is_deleted\" = false");
        }
    }
}
