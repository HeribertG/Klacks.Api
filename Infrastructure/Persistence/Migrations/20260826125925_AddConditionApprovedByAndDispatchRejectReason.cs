using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionApprovedByAndDispatchRejectReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "reject_reason",
                table: "agent_trigger_dispatches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "approved_by_user_id",
                table: "agent_conditions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reject_reason",
                table: "agent_trigger_dispatches");

            migrationBuilder.DropColumn(
                name: "approved_by_user_id",
                table: "agent_conditions");
        }
    }
}
