using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class DataSeeder
    {
        public static void Add(MigrationBuilder migrationBuilder, bool withFake = false)
        {
            Default.SeedData(migrationBuilder);
            
            if (withFake)
            {
                SwissZip.SeedData(migrationBuilder);
                CalendarRules.SeedData(migrationBuilder);
                AbsencesSeed.SeedData(migrationBuilder);
                Macros.SeedData(migrationBuilder);
                SwissCantonCalendarSeed.SeedCalendarSelections(migrationBuilder);
                FakeDatas.SeedData(migrationBuilder);
            }
        }
    }
}
