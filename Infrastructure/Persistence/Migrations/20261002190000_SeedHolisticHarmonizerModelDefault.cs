using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Installs gemini-25-flash as the Holistic Harmonizer model where none is chosen yet and the model is
    /// callable, and replaces the exact legacy value gemini-35-flash (a thinking model that failed the
    /// pre-flight ping). See HolisticHarmonizerModelDefaultSql for the guards.
    /// </summary>
    public partial class SeedHolisticHarmonizerModelDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            HolisticHarmonizerModelDefaultSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
