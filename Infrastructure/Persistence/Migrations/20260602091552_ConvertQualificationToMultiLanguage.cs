using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertQualificationToMultiLanguage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_qualification_name_is_deleted",
                table: "qualification");

            migrationBuilder.Sql(@"
                ALTER TABLE qualification
                ALTER COLUMN name TYPE jsonb
                USING jsonb_build_object('de', name, 'en', name, 'fr', name, 'it', name);
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE qualification
                ALTER COLUMN description TYPE jsonb
                USING CASE
                    WHEN description IS NULL THEN NULL
                    ELSE jsonb_build_object('de', description, 'en', description, 'fr', description, 'it', description)
                END;
            ");

            migrationBuilder.AlterColumn<string>(
                name: "emoji",
                table: "qualification",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE qualification
                ALTER COLUMN name TYPE character varying(150)
                USING name->>'de';
            ");

            migrationBuilder.Sql(@"
                ALTER TABLE qualification
                ALTER COLUMN description TYPE character varying(500)
                USING description->>'de';
            ");

            migrationBuilder.AlterColumn<string>(
                name: "emoji",
                table: "qualification",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_qualification_name_is_deleted",
                table: "qualification",
                columns: new[] { "name", "is_deleted" });
        }
    }
}
