using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDescriptionToBreak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "shift_id",
                table: "schedule_cells",
                newName: "entry_id");

            migrationBuilder.AddColumn<string>(
                name: "description_de",
                table: "break",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "break",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_fr",
                table: "break",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_it",
                table: "break",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_de",
                table: "absence_detail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_en",
                table: "absence_detail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_fr",
                table: "absence_detail",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description_it",
                table: "absence_detail",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "description_de",
                table: "break");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "break");

            migrationBuilder.DropColumn(
                name: "description_fr",
                table: "break");

            migrationBuilder.DropColumn(
                name: "description_it",
                table: "break");

            migrationBuilder.DropColumn(
                name: "description_de",
                table: "absence_detail");

            migrationBuilder.DropColumn(
                name: "description_en",
                table: "absence_detail");

            migrationBuilder.DropColumn(
                name: "description_fr",
                table: "absence_detail");

            migrationBuilder.DropColumn(
                name: "description_it",
                table: "absence_detail");

            migrationBuilder.RenameColumn(
                name: "entry_id",
                table: "schedule_cells",
                newName: "shift_id");
        }
    }
}
