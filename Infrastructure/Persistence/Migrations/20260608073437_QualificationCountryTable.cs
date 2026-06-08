using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QualificationCountryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "qualification_country",
                columns: table => new
                {
                    qualification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qualification_country", x => new { x.qualification_id, x.country_code });
                    table.ForeignKey(
                        name: "fk_qualification_country_qualification_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Migrate existing single-country values from the old scalar column
            migrationBuilder.Sql(@"
                INSERT INTO qualification_country (qualification_id, country_code)
                SELECT id, country
                FROM qualification
                WHERE country IS NOT NULL AND country <> ''
                ON CONFLICT DO NOTHING;
            ");

            // Deutsch (DE) also spoken in CH and AT
            migrationBuilder.Sql(@"
                INSERT INTO qualification_country (qualification_id, country_code)
                SELECT qc.qualification_id, unnest(ARRAY['CH','AT'])
                FROM qualification_country qc
                JOIN qualification q ON q.id = qc.qualification_id
                WHERE qc.country_code = 'DE' AND q.type = 1
                ON CONFLICT DO NOTHING;
            ");

            // Français (FR) also spoken in CH and BE
            migrationBuilder.Sql(@"
                INSERT INTO qualification_country (qualification_id, country_code)
                SELECT qc.qualification_id, unnest(ARRAY['CH','BE'])
                FROM qualification_country qc
                JOIN qualification q ON q.id = qc.qualification_id
                WHERE qc.country_code = 'FR' AND q.type = 1
                ON CONFLICT DO NOTHING;
            ");

            // Italiano (IT) also spoken in CH and SM
            migrationBuilder.Sql(@"
                INSERT INTO qualification_country (qualification_id, country_code)
                SELECT qc.qualification_id, unnest(ARRAY['CH','SM'])
                FROM qualification_country qc
                JOIN qualification q ON q.id = qc.qualification_id
                WHERE qc.country_code = 'IT' AND q.type = 1
                ON CONFLICT DO NOTHING;
            ");

            // English (GB) also spoken in US, AU, CA, CH, IE
            migrationBuilder.Sql(@"
                INSERT INTO qualification_country (qualification_id, country_code)
                SELECT qc.qualification_id, unnest(ARRAY['US','AU','CA','CH','IE'])
                FROM qualification_country qc
                JOIN qualification q ON q.id = qc.qualification_id
                WHERE qc.country_code = 'GB' AND q.type = 1
                ON CONFLICT DO NOTHING;
            ");

            migrationBuilder.DropColumn(
                name: "country",
                table: "qualification");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "country",
                table: "qualification",
                type: "text",
                nullable: true);

            // Restore first country per qualification as the scalar value
            migrationBuilder.Sql(@"
                UPDATE qualification q
                SET country = (
                    SELECT country_code
                    FROM qualification_country qc
                    WHERE qc.qualification_id = q.id
                    ORDER BY country_code
                    LIMIT 1
                );
            ");

            migrationBuilder.DropTable(
                name: "qualification_country");
        }
    }
}
