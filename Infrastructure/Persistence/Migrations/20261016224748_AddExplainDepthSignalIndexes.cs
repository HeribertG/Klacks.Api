using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExplainDepthSignalIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_work_current_user_created_create_time",
                table: "work",
                columns: new[] { "current_user_created", "create_time" },
                filter: "analyse_token IS NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_skill_usage_records_user_id",
                table: "skill_usage_records",
                column: "user_id",
                filter: "success = true AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_current_user_created_create_time",
                table: "work");

            migrationBuilder.DropIndex(
                name: "ix_skill_usage_records_user_id",
                table: "skill_usage_records");
        }
    }
}
