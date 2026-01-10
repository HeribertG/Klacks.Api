using Klacks.Api.Data.Seed.IdentityProviders;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class DataSeeder
    {
        public static void Add(MigrationBuilder migrationBuilder, bool withFake = false)
        {
            DefaultSeed.SeedData(migrationBuilder);
            LLMSeed.SeedData(migrationBuilder);
            SwissZipSeed.SeedData(migrationBuilder);
            MacrosSeed.SeedData(migrationBuilder);
            IdentityProvidersSeed.SeedData(migrationBuilder);

            if (withFake)
            {
                CalendarRulesSeed.SeedData(migrationBuilder);
                AbsencesSeed.SeedData(migrationBuilder);
                SwissCantonCalendarSelectionsSeed.SeedCalendarSelections(migrationBuilder);
                FakeDataSeed.SeedData(migrationBuilder);
                ContractsSeed.SeedContracts(migrationBuilder);
            }
        }
    }
}
