using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProactiveDispatchReminder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_agent_trigger_dispatches_user_id_trigger_kind_dedup_key",
                table: "agent_trigger_dispatches");

            migrationBuilder.AddColumn<DateTime>(
                name: "acknowledged_at_utc",
                table: "agent_trigger_dispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_reminded_at_utc",
                table: "agent_trigger_dispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "next_reminder_at_utc",
                table: "agent_trigger_dispatches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reminder_count",
                table: "agent_trigger_dispatches",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Rows the user already reacted to count as acknowledged, so the reminder loop does not
            // resurrect old dispatches. AcknowledgedAtUtc stays null for rows never reacted to.
            migrationBuilder.Sql(
                "UPDATE agent_trigger_dispatches SET acknowledged_at_utc = reaction_at_utc WHERE reaction <> 0 AND acknowledged_at_utc IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_dispatches_dedup_linked",
                table: "agent_trigger_dispatches",
                columns: new[] { "user_id", "trigger_kind", "dedup_key", "condition_id" },
                unique: true,
                filter: "\"is_deleted\" = false AND \"condition_id\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_dispatches_dedup_unlinked",
                table: "agent_trigger_dispatches",
                columns: new[] { "user_id", "trigger_kind", "dedup_key" },
                unique: true,
                filter: "\"is_deleted\" = false AND \"condition_id\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_dispatches_reminder_due",
                table: "agent_trigger_dispatches",
                column: "next_reminder_at_utc",
                filter: "\"next_reminder_at_utc\" IS NOT NULL AND \"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_agent_trigger_dispatches_dedup_linked",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropIndex(
                name: "ix_agent_trigger_dispatches_dedup_unlinked",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropIndex(
                name: "ix_agent_trigger_dispatches_reminder_due",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "acknowledged_at_utc",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "last_reminded_at_utc",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "next_reminder_at_utc",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "reminder_count",
                table: "agent_trigger_dispatches");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_dispatches_user_id_trigger_kind_dedup_key",
                table: "agent_trigger_dispatches",
                columns: new[] { "user_id", "trigger_kind", "dedup_key" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }
    }
}
