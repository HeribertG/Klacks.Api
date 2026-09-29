// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Data.Seed.IdentityProviders;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class DataSeeder
    {
        public static void Add(MigrationBuilder migrationBuilder, bool withFake = false, bool withDemoShiftsAndGroups = true, string language = "de")
        {
            DefaultSeed.SeedData(migrationBuilder);
            LLMSeed.SeedData(migrationBuilder);
            TranscriptionDictionarySeed.SeedData(migrationBuilder);
            SwissZipSeed.SeedData(migrationBuilder);
            MacrosSeed.SeedData(migrationBuilder);
            CalendarRulesSeed.SeedData(migrationBuilder);
            AdditionalCalendarRulesSeed.SeedData(migrationBuilder);
            UnofficialHolidayDescriptionsSql.Apply(migrationBuilder);
            AbsencesSeed.SeedData(migrationBuilder);
            QualificationsSeed.SeedData(migrationBuilder);
            ReportTemplatesSeed.SeedData(migrationBuilder);
            SwissCantonCalendarSelectionsSeed.SeedCalendarSelections(migrationBuilder);
            CountryCalendarSelectionsSeed.SeedCalendarSelections(migrationBuilder);

            if (withFake)
            {
                IdentityProvidersSeed.SeedData(migrationBuilder);
                ContractsSeed.SeedContracts(migrationBuilder);
                FakeDataSeed.SeedData(migrationBuilder, withDemoShiftsAndGroups, language);
            }
        }
    }
}
