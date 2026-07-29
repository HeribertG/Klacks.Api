using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanOriginAndCandidatePlanLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "plan_id",
                table: "goal_candidates",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "origin",
                table: "agent_plans",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "user_goal");

            migrationBuilder.CreateIndex(
                name: "ix_goal_candidates_plan_id",
                table: "goal_candidates",
                column: "plan_id",
                filter: "plan_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_goal_candidates_plan_id",
                table: "goal_candidates");

            migrationBuilder.DropColumn(
                name: "plan_id",
                table: "goal_candidates");

            migrationBuilder.DropColumn(
                name: "origin",
                table: "agent_plans");
        }
    }
}
