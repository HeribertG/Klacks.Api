using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddClientPhoneticTokensAndTrigram : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "phonetic_tokens",
                table: "client",
                type: "character varying(400)",
                maxLength: 400,
                nullable: true);

            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // Expression must match the fuzzy search query verbatim, otherwise the GIN index is
            // never used. lower() on both sides keeps trigram matching case-insensitive.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_client_search_text_trgm ON client USING gin " +
                "((lower(coalesce(name, '') || ' ' || coalesce(first_name, '') || ' ' || " +
                "coalesce(maiden_name, '') || ' ' || coalesce(company, ''))) gin_trgm_ops);");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_client_phonetic_tokens ON client USING gin " +
                "((string_to_array(phonetic_tokens, ' ')));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_client_phonetic_tokens;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_client_search_text_trgm;");

            migrationBuilder.DropColumn(
                name: "phonetic_tokens",
                table: "client");
        }
    }
}
