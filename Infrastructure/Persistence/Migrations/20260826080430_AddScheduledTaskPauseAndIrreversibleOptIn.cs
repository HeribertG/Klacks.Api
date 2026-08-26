using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduledTaskPauseAndIrreversibleOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_scheduled_tasks_is_enabled_next_run_utc",
                table: "scheduled_tasks");

            migrationBuilder.AddColumn<bool>(
                name: "allow_irreversible_unattended",
                table: "scheduled_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_paused",
                table: "scheduled_tasks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "paused_reason",
                table: "scheduled_tasks",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_is_enabled_is_paused_next_run_utc",
                table: "scheduled_tasks",
                columns: new[] { "is_enabled", "is_paused", "next_run_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_scheduled_tasks_is_enabled_is_paused_next_run_utc",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "allow_irreversible_unattended",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "is_paused",
                table: "scheduled_tasks");

            migrationBuilder.DropColumn(
                name: "paused_reason",
                table: "scheduled_tasks");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_is_enabled_next_run_utc",
                table: "scheduled_tasks",
                columns: new[] { "is_enabled", "next_run_utc" });
        }
    }
}
