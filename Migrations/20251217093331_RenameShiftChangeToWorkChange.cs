using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameShiftChangeToWorkChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "shift_change",
                newName: "work_change");

            migrationBuilder.RenameIndex(
                name: "ix_shift_change_work_id",
                table: "work_change",
                newName: "ix_work_change_work_id");

            migrationBuilder.RenameIndex(
                name: "ix_shift_change_replace_client_id",
                table: "work_change",
                newName: "ix_work_change_replace_client_id");

            migrationBuilder.Sql("ALTER TABLE work_change RENAME CONSTRAINT pk_shift_change TO pk_work_change");
            migrationBuilder.Sql("ALTER TABLE work_change RENAME CONSTRAINT fk_shift_change_client_replace_client_id TO fk_work_change_client_replace_client_id");
            migrationBuilder.Sql("ALTER TABLE work_change RENAME CONSTRAINT fk_shift_change_work_work_id TO fk_work_change_work_work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE work_change RENAME CONSTRAINT fk_work_change_work_work_id TO fk_shift_change_work_work_id");
            migrationBuilder.Sql("ALTER TABLE work_change RENAME CONSTRAINT fk_work_change_client_replace_client_id TO fk_shift_change_client_replace_client_id");
            migrationBuilder.Sql("ALTER TABLE work_change RENAME CONSTRAINT pk_work_change TO pk_shift_change");

            migrationBuilder.RenameIndex(
                name: "ix_work_change_replace_client_id",
                table: "work_change",
                newName: "ix_shift_change_replace_client_id");

            migrationBuilder.RenameIndex(
                name: "ix_work_change_work_id",
                table: "work_change",
                newName: "ix_shift_change_work_id");

            migrationBuilder.RenameTable(
                name: "work_change",
                newName: "shift_change");
        }
    }
}
