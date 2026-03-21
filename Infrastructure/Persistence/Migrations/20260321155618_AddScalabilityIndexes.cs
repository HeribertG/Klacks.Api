using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScalabilityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_change_work_id",
                table: "work_change");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_work_id_is_deleted",
                table: "work_change",
                columns: new[] { "work_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_work_current_date_client_id_is_deleted",
                table: "work",
                columns: new[] { "current_date", "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_break_current_date_client_id",
                table: "break",
                columns: new[] { "current_date", "client_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_work_change_work_id_is_deleted",
                table: "work_change");

            migrationBuilder.DropIndex(
                name: "ix_work_current_date_client_id_is_deleted",
                table: "work");

            migrationBuilder.DropIndex(
                name: "ix_break_current_date_client_id",
                table: "break");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_work_id",
                table: "work_change",
                column: "work_id");
        }
    }
}
