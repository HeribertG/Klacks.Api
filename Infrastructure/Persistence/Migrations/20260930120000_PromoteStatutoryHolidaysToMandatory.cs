using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Marks the seeded calendar rules that are statutory but were seeded as non-statutory as mandatory on
    /// existing databases: Josefstag in NW, SZ, TI, UR, VS, Peter und Paul in TI and the six US federal
    /// holidays that were flagged false. The US rows lose the generated "unofficial" description only while
    /// it is still exactly that text. Rows are addressed by seed id. Data only - the model is unchanged.
    /// </summary>
    public partial class PromoteStatutoryHolidaysToMandatory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            StatutoryHolidayPromotionSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            StatutoryHolidayPromotionSql.Remove(migrationBuilder);
        }
    }
}