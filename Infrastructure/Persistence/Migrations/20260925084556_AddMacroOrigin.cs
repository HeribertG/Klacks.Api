using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroOrigin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "origin",
                table: "macro",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"UPDATE public.macro SET origin = 1
WHERE id IN (
    'b1481e19-eaba-458a-a33b-666f2ecc28d2',
    'ac8a7b05-2312-41aa-a21d-e3edba54aef5',
    'a3edd3f5-c31c-4746-a9a0-c613d14ffd23',
    'e4a71d2c-5b8f-4c3a-9d16-84f0b2a7c9e3',
    'ad86380e-3e8e-4497-95c1-3555ee0803c4',
    'f7704df2-bb51-40c8-9ecd-ad57c1064490',
    '9f2b4c67-3d1a-4e85-b7c9-5a8d0e6f2b31',
    '3bac9e54-4368-4174-8bc9-435ce08aecbd',
    '7c5a9d21-4e8b-4f3a-9c67-2d1e8f5b0a43');");

            migrationBuilder.Sql(@"UPDATE public.macro SET origin = 2
WHERE origin = 0 AND import_source_key <> '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "origin",
                table: "macro");
        }
    }
}
