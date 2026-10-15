using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEscalationChainScenarioWakeAndExhaustedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "exhausted_at_utc",
                table: "escalation_chains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scenario_id",
                table: "escalation_chains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "wake_not_before_utc",
                table: "escalation_chains",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_escalation_chains_scenario_id",
                table: "escalation_chains",
                column: "scenario_id");

            migrationBuilder.Sql(
                "UPDATE escalation_chains SET exhausted_at_utc = LEAST(deadline_utc, now()) " +
                "WHERE status = 2 AND exhausted_at_utc IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_escalation_chains_scenario_id",
                table: "escalation_chains");

            migrationBuilder.DropColumn(
                name: "exhausted_at_utc",
                table: "escalation_chains");

            migrationBuilder.DropColumn(
                name: "scenario_id",
                table: "escalation_chains");

            migrationBuilder.DropColumn(
                name: "wake_not_before_utc",
                table: "escalation_chains");
        }
    }
}
