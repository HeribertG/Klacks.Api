using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class ConvertBreakDescriptionToJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "macro_type");

            migrationBuilder.AddColumn<string>(
                name: "information",
                table: "schedule_cells",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "break",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE break
                SET description = jsonb_build_object(
                    'De', description_de,
                    'En', description_en,
                    'Fr', description_fr,
                    'It', description_it
                )
                WHERE description_de IS NOT NULL
                   OR description_en IS NOT NULL
                   OR description_fr IS NOT NULL
                   OR description_it IS NOT NULL;
            ");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "information",
                table: "schedule_cells");

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

            migrationBuilder.Sql(@"
                UPDATE break
                SET description_de = description->>'De',
                    description_en = description->>'En',
                    description_fr = description->>'Fr',
                    description_it = description->>'It'
                WHERE description IS NOT NULL;
            ");

            migrationBuilder.DropColumn(
                name: "description",
                table: "break");

            migrationBuilder.CreateTable(
                name: "macro_type",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_macro_type", x => x.id);
                });
        }
    }
}
