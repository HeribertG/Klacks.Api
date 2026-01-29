using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    public partial class ConvertAllMultiLanguageToJsonb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // State: name
            MigrateToJsonb(migrationBuilder, "state", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });

            // Macro: description
            MigrateToJsonb(migrationBuilder, "macro", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });

            // Countries: name
            MigrateToJsonb(migrationBuilder, "countries", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });

            // CalendarRule: name, description
            MigrateToJsonb(migrationBuilder, "calendar_rule", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });
            MigrateToJsonb(migrationBuilder, "calendar_rule", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });

            // AbsenceDetail: detail_name, description
            MigrateToJsonb(migrationBuilder, "absence_detail", "detail_name", new[] { "detail_name_de", "detail_name_en", "detail_name_fr", "detail_name_it" });
            MigrateToJsonb(migrationBuilder, "absence_detail", "description", new[] { "description_de", "description_en", "description_fr", "description_it" }, isNullable: true);

            // Absence: name, description, abbreviation
            MigrateToJsonb(migrationBuilder, "absence", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });
            MigrateToJsonb(migrationBuilder, "absence", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });
            MigrateToJsonb(migrationBuilder, "absence", "abbreviation", new[] { "abbreviation_de", "abbreviation_en", "abbreviation_fr", "abbreviation_it" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // State: name
            MigrateFromJsonb(migrationBuilder, "state", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });

            // Macro: description
            MigrateFromJsonb(migrationBuilder, "macro", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });

            // Countries: name
            MigrateFromJsonb(migrationBuilder, "countries", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });

            // CalendarRule: name, description
            MigrateFromJsonb(migrationBuilder, "calendar_rule", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });
            MigrateFromJsonb(migrationBuilder, "calendar_rule", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });

            // AbsenceDetail: detail_name, description
            MigrateFromJsonb(migrationBuilder, "absence_detail", "detail_name", new[] { "detail_name_de", "detail_name_en", "detail_name_fr", "detail_name_it" });
            MigrateFromJsonb(migrationBuilder, "absence_detail", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });

            // Absence: name, description, abbreviation
            MigrateFromJsonb(migrationBuilder, "absence", "name", new[] { "name_de", "name_en", "name_fr", "name_it" });
            MigrateFromJsonb(migrationBuilder, "absence", "description", new[] { "description_de", "description_en", "description_fr", "description_it" });
            MigrateFromJsonb(migrationBuilder, "absence", "abbreviation", new[] { "abbreviation_de", "abbreviation_en", "abbreviation_fr", "abbreviation_it" });
        }

        private static void MigrateToJsonb(MigrationBuilder migrationBuilder, string table, string newColumn, string[] oldColumns, bool isNullable = false)
        {
            migrationBuilder.AddColumn<string>(
                name: newColumn,
                table: table,
                type: "jsonb",
                nullable: isNullable,
                defaultValue: "{}");

            var langKeys = new[] { "de", "en", "fr", "it" };
            var setters = new List<string>();
            for (int i = 0; i < oldColumns.Length && i < langKeys.Length; i++)
            {
                setters.Add($"'{langKeys[i]}', {oldColumns[i]}");
            }

            var whereConditions = string.Join(" OR ", oldColumns.Select(c => $"{c} IS NOT NULL"));

            migrationBuilder.Sql($@"
                UPDATE {table}
                SET {newColumn} = jsonb_strip_nulls(jsonb_build_object({string.Join(", ", setters)}))
                WHERE {whereConditions};
            ");

            foreach (var col in oldColumns)
            {
                migrationBuilder.DropColumn(name: col, table: table);
            }
        }

        private static void MigrateFromJsonb(MigrationBuilder migrationBuilder, string table, string jsonColumn, string[] oldColumns)
        {
            var langKeys = new[] { "de", "en", "fr", "it" };

            foreach (var col in oldColumns)
            {
                migrationBuilder.AddColumn<string>(
                    name: col,
                    table: table,
                    type: "text",
                    nullable: true);
            }

            var setStatements = new List<string>();
            for (int i = 0; i < oldColumns.Length && i < langKeys.Length; i++)
            {
                setStatements.Add($"{oldColumns[i]} = {jsonColumn}->>'{langKeys[i]}'");
            }

            migrationBuilder.Sql($@"
                UPDATE {table}
                SET {string.Join(", ", setStatements)}
                WHERE {jsonColumn} IS NOT NULL;
            ");

            migrationBuilder.DropColumn(name: jsonColumn, table: table);
        }
    }
}
