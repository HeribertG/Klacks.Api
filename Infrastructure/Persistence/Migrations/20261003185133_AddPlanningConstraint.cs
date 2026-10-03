using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanningConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "approval_status",
                table: "counter_rule",
                type: "integer",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "origin",
                table: "counter_rule",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "source_text",
                table: "counter_rule",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "planning_constraint",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false),
                    scope_type = table.Column<int>(type: "integer", nullable: false),
                    scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parameters_json = table.Column<string>(type: "jsonb", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    origin = table.Column<int>(type: "integer", nullable: false),
                    approval_status = table.Column<int>(type: "integer", nullable: false),
                    source_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    paraphrase = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    proposed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    approved_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    approved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
                    previous_version_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_planning_constraint", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_planning_constraint_analyse_token",
                table: "planning_constraint",
                column: "analyse_token");

            migrationBuilder.CreateIndex(
                name: "ix_planning_constraint_approval_status_valid_from_valid_until",
                table: "planning_constraint",
                columns: new[] { "approval_status", "valid_from", "valid_until" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_planning_constraint_import_source_key",
                table: "planning_constraint",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false AND import_source_key <> ''");

            migrationBuilder.CreateIndex(
                name: "ix_planning_constraint_scope_type_scope_id",
                table: "planning_constraint",
                columns: new[] { "scope_type", "scope_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planning_constraint");

            migrationBuilder.DropColumn(
                name: "approval_status",
                table: "counter_rule");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "counter_rule");

            migrationBuilder.DropColumn(
                name: "source_text",
                table: "counter_rule");
        }
    }
}
