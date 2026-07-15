using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixNullHourMacroDimHour : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data fix for existing installations: the seeded "Null Hour" macro carried a stray
            // "Dim Hour" line that re-declares the imported Hour symbol - the script parser chokes on
            // that redeclaration. MacrosSeed.cs is already fixed for fresh databases; this idempotent
            // UPDATE repairs rows seeded before the fix. Matched by the fixed seed row id OR by name
            // (dev databases accumulate duplicate seeded rows from test runs), guarded by the LIKE so
            // re-runs and already-clean rows are no-ops. Both CRLF and LF variants are stripped because
            // the seeded content's line endings depend on the checkout that built the running binary.
            migrationBuilder.Sql(
                @"UPDATE public.macro
SET content = replace(replace(replace(content, E'Dim Hour\r\n', ''), E'Dim Hour\n', ''), 'Dim Hour', '')
WHERE (id = 'f7704df2-bb51-40c8-9ecd-ad57c1064490' OR name = 'Null Hour')
  AND content LIKE '%Dim Hour%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Deliberately empty: re-inserting a known-broken script line has no rollback value.
        }
    }
}
