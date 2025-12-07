using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class FakeDataSeed
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
                var scriptForBreakPlaceholders = SeedGenerator.GenerateInsertScriptForBreakPlaceholders(results.BreakPlaceholders);
                var scriptForSettings = SeedGenerator.GenerateInsertScriptForSettings();
                var scriptForGroups = GroupsSeed.GenerateInsertScriptForGroups();
                var scriptForGroupItems = SeedGenerator.GenerateInsertScriptForGroupItems(results.Clients, results.Addresses);
                var (scriptForShifts, shiftIds) = ShiftSeed.GenerateInsertScriptForShifts();
                var (scriptForContainerTemplates, containerTemplateIds) = ShiftSeed.GenerateContainerTemplates();
                var (scriptForContainers, containerIds) = ShiftSeed.GenerateContainers();
                var (scriptForTimeRangeShifts, timeRangeShiftIds) = ShiftSeed.GenerateTimeRangeShiftsWithClients();

                var allShiftIds = shiftIds.Concat(containerTemplateIds).Concat(containerIds).Concat(timeRangeShiftIds).ToList();
                var scriptForShiftGroupItems = ShiftSeed.GenerateInsertScriptForShiftGroupItems(allShiftIds);

                migrationBuilder.Sql(scriptForClients);
                migrationBuilder.Sql(scriptForAddresses);
                migrationBuilder.Sql(scriptForMemberships);
                migrationBuilder.Sql(scriptForCommunications);
                migrationBuilder.Sql(scriptForAnnotations);
                migrationBuilder.Sql(scriptForBreakPlaceholders);
                migrationBuilder.Sql(scriptForSettings);
                migrationBuilder.Sql(scriptForGroups);
                migrationBuilder.Sql(scriptForGroupItems);
                migrationBuilder.Sql(scriptForShifts);
                migrationBuilder.Sql(scriptForContainerTemplates);
                migrationBuilder.Sql(scriptForContainers);
                migrationBuilder.Sql(scriptForTimeRangeShifts);
                migrationBuilder.Sql(scriptForShiftGroupItems);
            }
        }
    }
}