using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeAnswerGroundingCounterIndexPartial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_answer_grounding_daily_counters_day_agent_id_evaluator_vers",
                table: "answer_grounding_daily_counters");

            migrationBuilder.CreateIndex(
                name: "ix_answer_grounding_daily_counters_day_agent_id_evaluator_vers",
                table: "answer_grounding_daily_counters",
                columns: new[] { "day", "agent_id", "evaluator_version" },
                unique: true,
                filter: "\"is_deleted\" = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_answer_grounding_daily_counters_day_agent_id_evaluator_vers",
                table: "answer_grounding_daily_counters");

            migrationBuilder.CreateIndex(
                name: "ix_answer_grounding_daily_counters_day_agent_id_evaluator_vers",
                table: "answer_grounding_daily_counters",
                columns: new[] { "day", "agent_id", "evaluator_version" },
                unique: true);
        }
    }
}
