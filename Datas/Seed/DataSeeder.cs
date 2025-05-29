using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class DataSeeder
    {
        public static void Add(MigrationBuilder migrationBuilder)
        {
            Default.SeedData(migrationBuilder);
            SwissZip.SeedData(migrationBuilder);
            CalendarRules.SeedData(migrationBuilder);
            AbsencesSeed.SeedData(migrationBuilder);
            Macros.SeedData(migrationBuilder);
            FakeDatas.SeedData(migrationBuilder);
        }
    }
}
