using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLLMSyncNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_group_item_shift_id",
                table: "group_item");

            migrationBuilder.CreateTable(
                name: "llm_sync_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    new_models_count = table.Column<int>(type: "integer", nullable: false),
                    deactivated_models_count = table.Column<int>(type: "integer", nullable: false),
                    new_model_names = table.Column<string>(type: "jsonb", nullable: false),
                    deactivated_model_names = table.Column<string>(type: "jsonb", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_llm_sync_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wizard_training_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    stage0violations = table.Column<int>(type: "integer", nullable: false),
                    stage1completion = table.Column<double>(type: "double precision", nullable: false),
                    stage2score = table.Column<double>(type: "double precision", nullable: false),
                    token_count = table.Column<int>(type: "integer", nullable: false),
                    available_shift_slots = table.Column<int>(type: "integer", nullable: false),
                    coverage_ratio = table.Column<double>(type: "double precision", nullable: false),
                    client_day_duplicates = table.Column<int>(type: "integer", nullable: false),
                    agents_count = table.Column<int>(type: "integer", nullable: false),
                    shifts_count = table.Column<int>(type: "integer", nullable: false),
                    period_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_wizard_training_runs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_training_runs_create_time",
                table: "wizard_training_runs",
                column: "create_time");

            migrationBuilder.CreateIndex(
                name: "ix_wizard_training_runs_source_create_time",
                table: "wizard_training_runs",
                columns: new[] { "source", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_training_runs_stage2score",
                table: "wizard_training_runs",
                column: "stage2score");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "llm_sync_notifications");

            migrationBuilder.DropTable(
                name: "wizard_training_runs");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_shift_id",
                table: "group_item",
                column: "shift_id");
        }
    }
}
