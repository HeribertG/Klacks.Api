// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Data.Seed.IdentityProviders;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class DataSeeder
    {
        public static void Add(MigrationBuilder migrationBuilder, bool withFake = false, string language = "de")
        {
            DefaultSeed.SeedData(migrationBuilder);
            LLMSeed.SeedData(migrationBuilder);
            TranscriptionDictionarySeed.SeedData(migrationBuilder);
            SwissZipSeed.SeedData(migrationBuilder);
            MacrosSeed.SeedData(migrationBuilder);
            CalendarRulesSeed.SeedData(migrationBuilder);
            AdditionalCalendarRulesSeed.SeedData(migrationBuilder);
            AbsencesSeed.SeedData(migrationBuilder);
            QualificationsSeed.SeedData(migrationBuilder);
            ReportTemplatesSeed.SeedData(migrationBuilder);
            SwissCantonCalendarSelectionsSeed.SeedCalendarSelections(migrationBuilder);
            CountryCalendarSelectionsSeed.SeedCalendarSelections(migrationBuilder);

            if (withFake)
            {
                IdentityProvidersSeed.SeedData(migrationBuilder);
                ContractsSeed.SeedContracts(migrationBuilder);
                FakeDataSeed.SeedData(migrationBuilder, language);
            }
        }
    }
}
