using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Repairs the name of the 13 seeded Berchtoldstag rules (2 January) on existing databases: English and the
    /// plugin languages called the day after a non-existent "Saint Berchtold". The seed inserts once and the plugin
    /// language merge only fills empty values, so the corrected texts never reached existing rows. A name is
    /// replaced only while it still equals the faulty text exactly, so an administrator's own name survives. Data
    /// only - the model is unchanged. Down is empty on purpose: it would reintroduce the faulty texts.
    /// </summary>
    public partial class BackfillSeededBerchtoldstagNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeededNameCorrectionSql.Apply(migrationBuilder, CalendarRuleNameCorrectionSql.Corrections);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
