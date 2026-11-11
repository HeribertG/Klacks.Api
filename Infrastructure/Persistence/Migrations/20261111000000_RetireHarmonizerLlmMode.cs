using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Pins wizard stage 3 (Holistic Harmonizer) to the deterministic local search. The language-model mode is retired
    /// (owner decision 2026-11-11) and its settings card is gone, so an installation that still stores 'llm' in
    /// WIZARD3_MODE would otherwise keep running the LLM engine with no screen left to switch it back.
    /// The literals are frozen on purpose: a migration must keep meaning what it meant when it was written.
    /// Down is empty: the previous value is not known, and the deterministic mode is the shipped default anyway.
    /// </summary>
    public partial class RetireHarmonizerLlmMode : Migration
    {
        public const string UpSql = """
            UPDATE settings SET value = 'deterministic'
            WHERE type = 'WIZARD3_MODE' AND lower(trim(value)) = 'llm';
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(UpSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
