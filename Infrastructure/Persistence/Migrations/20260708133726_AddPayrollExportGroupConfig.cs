using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollExportGroupConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payroll_export_group_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_system = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    delimiter = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    encoding = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_wage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    surcharge_wage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    absence_mapping_json = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_payroll_export_group_config", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payroll_export_group_config_group_id",
                table: "payroll_export_group_config",
                column: "group_id",
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payroll_export_group_config");
        }
    }
}
