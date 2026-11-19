using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Switches off the seeded public Forumsys test directory for sign-in. It was seeded enabled with
    /// use_for_authentication, so every failed local login sent the typed password in clear text (port 389) to
    /// ldap.forumsys.com. Only the seeded row is touched, and only while it still points to that host, so a row an
    /// administrator re-pointed to an own server keeps its settings. The literals are frozen on purpose: a migration
    /// must keep meaning what it meant when it was written. Down is empty: re-enabling would bring the leak back.
    /// </summary>
    public partial class DisableSeededForumsysDirectoryLogin : Migration
    {
        public const string UpSql = """
            UPDATE identity_providers SET use_for_authentication = false, is_enabled = false WHERE id = 'fc0b8e9c-4694-40c1-a10e-fdb66a10b26e' AND host = 'ldap.forumsys.com';
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
