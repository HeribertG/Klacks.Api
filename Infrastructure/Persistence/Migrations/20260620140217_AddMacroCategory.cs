using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category",
                table: "macro",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // One-time backfill of the category for the standard macros (lookup at runtime is by the
            // category field, never by name). One macro per category keeps the default unambiguous.
            migrationBuilder.Sql("UPDATE macro SET category = 1 WHERE lower(name) = 'allshift';");
            migrationBuilder.Sql("UPDATE macro SET category = 2 WHERE lower(name) = 'vacation';");
            migrationBuilder.Sql("UPDATE macro SET category = 4 WHERE lower(name) = 'accident';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "category",
                table: "macro");
        }
    }
}
