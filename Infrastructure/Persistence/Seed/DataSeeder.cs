using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class DataSeeder
    {
        public static void Add(MigrationBuilder migrationBuilder, bool withFake = false)
        {
            DefaultSeed.SeedData(migrationBuilder);
            LLMSeed.SeedData(migrationBuilder);
            
            if (withFake)
            {
                SwissZipSeed.SeedData(migrationBuilder);
                CalendarRulesSeed.SeedData(migrationBuilder);
                AbsencesSeed.SeedData(migrationBuilder);
                MacrosSeed.SeedData(migrationBuilder);
                SwissCantonCalendarSelectionsSeed.SeedCalendarSelections(migrationBuilder);
                FakeDataSeed.SeedData(migrationBuilder);
                ContractsSeed.SeedContracts(migrationBuilder);
            }
        }
    }
}
