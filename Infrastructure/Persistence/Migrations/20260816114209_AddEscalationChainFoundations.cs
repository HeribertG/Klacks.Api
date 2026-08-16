using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationChainFoundations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "escalation_chains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    absent_client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    absent_client_name = table.Column<string>(type: "text", nullable: false),
                    absence_break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    deadline_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    acknowledged_by_user_id = table.Column<string>(type: "text", nullable: true),
                    acknowledged_by_user_name = table.Column<string>(type: "text", nullable: true),
                    acknowledged_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_by_user_id = table.Column<string>(type: "text", nullable: true),
                    cancelled_by_user_name = table.Column<string>(type: "text", nullable: true),
                    cancel_reason = table.Column<string>(type: "text", nullable: true),
                    cancelled_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    outcome_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_escalation_chains", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "escalation_roster_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_root_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    derived_rank = table.Column<int>(type: "integer", nullable: true),
                    override_rank = table.Column<int>(type: "integer", nullable: true),
                    is_orphaned = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_escalation_roster_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "escalation_stages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    escalation_chain_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    user_display_name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    skip_reason = table.Column<string>(type: "text", nullable: true),
                    notified_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    due_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    delivery_channel = table.Column<string>(type: "text", nullable: true),
                    delivery_outcome = table.Column<string>(type: "text", nullable: true),
                    dispatch_row_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_escalation_stages", x => x.id);
                    table.ForeignKey(
                        name: "fk_escalation_stages_escalation_chains_escalation_chain_id",
                        column: x => x.escalation_chain_id,
                        principalTable: "escalation_chains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_absence_break_id",
                table: "escalation_chains",
                column: "absence_break_id");

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_status",
                table: "escalation_chains",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_work_id",
                table: "escalation_chains",
                column: "work_id",
                unique: true,
                filter: "\"is_deleted\" = false AND \"status\" = 0");

            migrationBuilder.CreateIndex(
                name: "ix_escalation_roster_entries_group_root_id_user_id",
                table: "escalation_roster_entries",
                columns: new[] { "group_root_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_escalation_stages_due_at_utc",
                table: "escalation_stages",
                column: "due_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_escalation_stages_escalation_chain_id_rank",
                table: "escalation_stages",
                columns: new[] { "escalation_chain_id", "rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_escalation_stages_user_id_status",
                table: "escalation_stages",
                columns: new[] { "user_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "escalation_roster_entries");

            migrationBuilder.DropTable(
                name: "escalation_stages");

            migrationBuilder.DropTable(
                name: "escalation_chains");
        }
    }
}
