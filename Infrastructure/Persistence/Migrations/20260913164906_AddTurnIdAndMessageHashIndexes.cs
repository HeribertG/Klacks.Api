using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTurnIdAndMessageHashIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_skill_usage_records_turn_id",
                table: "skill_usage_records",
                column: "turn_id");

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_user_message_hash",
                table: "skill_selection_trajectories",
                column: "user_message_hash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_usage_records_turn_id",
                table: "skill_usage_records");

            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_user_message_hash",
                table: "skill_selection_trajectories");
        }
    }
}
