using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Repairs the seeded country names on existing databases: the French and Italian name of the USA carry a
    /// doubled apostrophe (four apostrophes in the SQL literal of the seed where two were meant). The seed
    /// inserts with ON CONFLICT DO NOTHING, so its corrected values never reached existing rows. A name is
    /// replaced only while it still equals the faulty seed text exactly. Data only - the model is unchanged.
    /// Down is empty on purpose: it would reintroduce the faulty texts.
    /// </summary>
    public partial class BackfillSeededCountryNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeededNameCorrectionSql.Apply(migrationBuilder, CountryNameCorrectionSql.Corrections);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
