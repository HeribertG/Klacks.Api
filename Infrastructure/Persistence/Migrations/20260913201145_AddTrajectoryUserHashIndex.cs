using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrajectoryUserHashIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_user_message_hash",
                table: "skill_selection_trajectories");

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_user_id_user_message_hash_crea",
                table: "skill_selection_trajectories",
                columns: new[] { "user_id", "user_message_hash", "create_time" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_selection_trajectories_user_id_user_message_hash_crea",
                table: "skill_selection_trajectories");

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_user_message_hash",
                table: "skill_selection_trajectories",
                column: "user_message_hash");
        }
    }
}
