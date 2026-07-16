using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWizardRunCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "wizard_run_capture",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    engine = table.Column<byte>(type: "smallint", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    run_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    scenario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    apply_kind = table.Column<byte>(type: "smallint", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    period_from = table.Column<DateOnly>(type: "date", nullable: false),
                    period_until = table.Column<DateOnly>(type: "date", nullable: false),
                    sub_score_json = table.Column<string>(type: "text", nullable: false),
                    stage0violations = table.Column<int>(type: "integer", nullable: false),
                    churn_at_apply = table.Column<double>(type: "double precision", nullable: true),
                    correction_churn = table.Column<double>(type: "double precision", nullable: true),
                    event_churn = table.Column<double>(type: "double precision", nullable: true),
                    measured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<byte>(type: "smallint", nullable: true),
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
                    table.PrimaryKey("pk_wizard_run_capture", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wizard_run_capture_work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    capture_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_wizard_run_capture_work", x => x.id);
                    table.ForeignKey(
                        name: "fk_wizard_run_capture_work_wizard_run_capture_capture_id",
                        column: x => x.capture_id,
                        principalTable: "wizard_run_capture",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_run_capture_is_deleted_group_id_period_from",
                table: "wizard_run_capture",
                columns: new[] { "is_deleted", "group_id", "period_from" });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_run_capture_is_deleted_job_id",
                table: "wizard_run_capture",
                columns: new[] { "is_deleted", "job_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_run_capture_is_deleted_scenario_id",
                table: "wizard_run_capture",
                columns: new[] { "is_deleted", "scenario_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_run_capture_work_capture_id",
                table: "wizard_run_capture_work",
                column: "capture_id");

            migrationBuilder.CreateIndex(
                name: "ix_wizard_run_capture_work_is_deleted_capture_id",
                table: "wizard_run_capture_work",
                columns: new[] { "is_deleted", "capture_id" });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_run_capture_work_work_id",
                table: "wizard_run_capture_work",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "wizard_run_capture_work");

            migrationBuilder.DropTable(
                name: "wizard_run_capture");
        }
    }
}
