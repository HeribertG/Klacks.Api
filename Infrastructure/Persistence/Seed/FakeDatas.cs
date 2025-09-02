using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class FakeDatas
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            if (!string.IsNullOrEmpty(FakeSettings.WithFake))
            {
                var number = int.Parse(FakeSettings.ClientsNumber);
                
                var results = SeedGenerator.GenerateClientsData(number, DateTime.Now.Year, true);

                var scriptForClients = SeedGenerator.GenerateInsertScriptForClients(results.Clients);
                var scriptForAddresses = SeedGenerator.GenerateInsertScriptForAddresses(results.Addresses);
                var scriptForMemberships = SeedGenerator.GenerateInsertScriptForMemberships(results.Memberships);
                var scriptForCommunications = SeedGenerator.GenerateInsertScriptForCommunications(results.Communications);
                var scriptForAnnotations = SeedGenerator.GenerateInsertScriptForAnnotations(results.Annotations);
                var scriptForBreaks = SeedGenerator.GenerateInsertScriptForBreaks(results.Breaks);
                var scriptForSettings = SeedGenerator.GenerateInsertScriptForSettings();
                var scriptForGroups = FakeDataGroups.GenerateInsertScriptForGroups();
                var scriptForGroupItems = SeedGenerator.GenerateInsertScriptForGroupItems(results.Clients, results.Addresses);
                var (scriptForShifts, shiftIds) = SeedGenerator.GenerateInsertScriptForShifts();
                var scriptForShiftGroupItems = SeedGenerator.GenerateInsertScriptForShiftGroupItems(shiftIds);
                
                migrationBuilder.Sql(scriptForClients);
                migrationBuilder.Sql(scriptForAddresses);
                migrationBuilder.Sql(scriptForMemberships);
                migrationBuilder.Sql(scriptForCommunications);
                migrationBuilder.Sql(scriptForAnnotations);
                migrationBuilder.Sql(scriptForBreaks);
                migrationBuilder.Sql(scriptForSettings);
                migrationBuilder.Sql(scriptForGroups);
                migrationBuilder.Sql(scriptForGroupItems);
                migrationBuilder.Sql(scriptForShifts);
                migrationBuilder.Sql(scriptForShiftGroupItems);
            }
        }
    }
}