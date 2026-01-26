using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAbbreviationToAbsence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "abbreviation_de",
                table: "absence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "abbreviation_en",
                table: "absence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "abbreviation_fr",
                table: "absence",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "abbreviation_it",
                table: "absence",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "abbreviation_de",
                table: "absence");

            migrationBuilder.DropColumn(
                name: "abbreviation_en",
                table: "absence");

            migrationBuilder.DropColumn(
                name: "abbreviation_fr",
                table: "absence");

            migrationBuilder.DropColumn(
                name: "abbreviation_it",
                table: "absence");
        }
    }
}
