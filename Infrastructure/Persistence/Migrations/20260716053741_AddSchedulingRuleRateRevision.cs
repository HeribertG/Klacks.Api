using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingRuleRateRevision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "scheduling_rule_rate_revisions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduling_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    night_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    holiday_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we1rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we2rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we3rate = table.Column<decimal>(type: "numeric", nullable: true),
                    import_source_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: ""),
                    import_content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: ""),
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
                    table.PrimaryKey("pk_scheduling_rule_rate_revisions", x => x.id);
                    table.ForeignKey(
                        name: "fk_scheduling_rule_rate_revisions_scheduling_rules_scheduling_",
                        column: x => x.scheduling_rule_id,
                        principalTable: "scheduling_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rule_rate_revisions_import_source_key",
                table: "scheduling_rule_rate_revisions",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rule_rate_revisions_scheduling_rule_id_valid_from",
                table: "scheduling_rule_rate_revisions",
                columns: new[] { "scheduling_rule_id", "valid_from" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "scheduling_rule_rate_revisions");
        }
    }
}
