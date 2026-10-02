using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Repairs the seeded qualification names on existing databases: four French names and one Italian name
    /// with doubled apostrophes and the French text in the Italian name of the cold chain qualification.
    /// The seed inserts with ON CONFLICT DO NOTHING, so its corrected values never reached existing rows.
    /// A name is replaced only while it still equals the faulty seed text exactly. Data only - the model is
    /// unchanged. Down is empty on purpose: it would reintroduce the faulty texts.
    /// </summary>
    public partial class BackfillSeededQualificationNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            QualificationNameCorrectionSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
