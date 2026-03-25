// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seeds fake client data either from a pre-generated SQL dump or dynamically.
/// Dump path loads geocoded addresses instantly; dynamic path generates fresh data.
/// </summary>

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class FakeDataSeed
    {
        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            if (!string.IsNullOrEmpty(FakeSettings.WithFake))
            {
                if (FakeSettings.UseDumpFile)
                {
                    SeedFromDump(migrationBuilder);
                }
                else
                {
                    SeedDynamically(migrationBuilder);
                }
            }
        }

        private static void SeedFromDump(MigrationBuilder migrationBuilder)
        {
            var scriptForSettings = SeedGenerator.GenerateInsertScriptForSettings();
            var scriptForGroups = GroupsSeed.GenerateInsertScriptForGroups();

            migrationBuilder.Sql(scriptForSettings);
            migrationBuilder.Sql(scriptForGroups);

            var dumpSql = StaticFakeDataLoader.LoadSeedDump("fake_seed_5000.sql");
            foreach (var chunk in SplitSqlIntoChunks(dumpSql, 1000))
            {
                migrationBuilder.Sql(chunk);
            }

            var scriptForBreakPlaceholders = GenerateBreakPlaceholdersSql();
            migrationBuilder.Sql(scriptForBreakPlaceholders);

            var (scriptForShifts, shiftIds) = ShiftSeed.GenerateInsertScriptForShifts();
            var (scriptForContainerTemplates, containerTemplateIds) = ShiftSeed.GenerateContainerTemplates();
            var (scriptForContainers, containerIds) = ShiftSeed.GenerateContainers();
            var (scriptForTimeRangeShifts, timeRangeShiftIds) = ShiftSeed.GenerateTimeRangeShiftsWithClients();

            var allShiftIds = shiftIds.Concat(containerTemplateIds).Concat(containerIds).Concat(timeRangeShiftIds).ToList();
            var scriptForShiftGroupItems = ShiftSeed.GenerateInsertScriptForShiftGroupItems(allShiftIds);

            migrationBuilder.Sql(scriptForShifts);
            migrationBuilder.Sql(scriptForContainerTemplates);
            migrationBuilder.Sql(scriptForContainers);
            migrationBuilder.Sql(scriptForTimeRangeShifts);
            migrationBuilder.Sql(scriptForShiftGroupItems);
        }

        private static string GenerateBreakPlaceholdersSql()
        {
            var maxBreaks = int.TryParse(FakeSettings.MaxBreaksPerClientPerYear, out var mb) ? mb : 30;
            var absenceIds = SeedGenerator.AbsenceIds;
            var sb = new System.Text.StringBuilder();
            var rand = new Random(42);
            var currentYear = DateTime.Now.Year;

            sb.AppendLine("-- Break placeholders (generated dynamically for dump-seeded clients)");
            sb.AppendLine(@"
DO $$
DECLARE
    rec RECORD;
    absence_ids UUID[] := ARRAY[" + string.Join(",", absenceIds.Select(id => $"'{id}'::UUID")) + @"];
    break_count INTEGER;
    i INTEGER;
    rand_absence UUID;
    break_from TIMESTAMP;
    break_until TIMESTAMP;
    rand_days INTEGER;
BEGIN
    FOR rec IN SELECT c.id AS client_id, m.valid_from FROM client c JOIN membership m ON m.client_id = c.id WHERE c.is_deleted = false AND m.is_deleted = false LOOP
        break_count := floor(random() * " + maxBreaks + @")::INTEGER;
        FOR i IN 1..break_count LOOP
            rand_absence := absence_ids[1 + floor(random() * array_length(absence_ids, 1))::INTEGER];
            rand_days := floor(random() * 730)::INTEGER;
            break_from := rec.valid_from + (rand_days || ' days')::INTERVAL + (floor(random() * 24) || ' hours')::INTERVAL;
            break_until := break_from + ((1 + floor(random() * 14)) || ' days')::INTERVAL;
            INSERT INTO break_placeholder (id, client_id, absence_id, ""from"", ""until"", information, is_deleted, create_time, current_user_created)
            VALUES (gen_random_uuid(), rec.client_id, rand_absence, break_from, break_until, '', false, NOW(), 'Anonymus');
        END LOOP;
    END LOOP;
END $$;");

            return sb.ToString();
        }

        private static void SeedDynamically(MigrationBuilder migrationBuilder)
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
        private static IEnumerable<string> SplitSqlIntoChunks(string sql, int statementsPerChunk)
        {
            var lines = sql.Split('\n');
            var chunk = new System.Text.StringBuilder();
            var statementCount = 0;

            foreach (var line in lines)
            {
                chunk.AppendLine(line);

                if (line.TrimEnd().EndsWith(";"))
                {
                    statementCount++;

                    if (statementCount >= statementsPerChunk)
                    {
                        yield return chunk.ToString();
                        chunk.Clear();
                        statementCount = 0;
                    }
                }
            }

            if (chunk.Length > 0)
            {
                yield return chunk.ToString();
            }
        }
    }
}
