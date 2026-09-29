using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Backfills the explanatory description of the seeded non-statutory calendar rules
    /// (is_mandatory = false) on existing databases, so the "unofficial" badge in the calendar rule list
    /// comes with a reason. Rows are addressed by seed id and only filled while their description is still
    /// empty; Down reverts only rows that still carry exactly the backfilled text. Data only - the model
    /// is unchanged.
    /// </summary>
    public partial class AddUnofficialHolidayDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            UnofficialHolidayDescriptionsSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            UnofficialHolidayDescriptionsSql.Remove(migrationBuilder);
        }
    }
}
