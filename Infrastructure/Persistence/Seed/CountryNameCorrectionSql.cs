// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Corrections for the seeded country names which older builds shipped with a doubled apostrophe: the French
/// and Italian name of the USA were written with four apostrophes in the SQL literal where two were meant, so
/// the stored text read "d''Amerique" instead of "d'Amerique". DefaultSeed inserts with ON CONFLICT DO NOTHING,
/// so existing databases never received the corrected seed values; the corrections are applied by the
/// BackfillSeededCountryNames migration. The faulty and corrected texts are frozen here on purpose: a migration
/// is history.
/// </summary>
namespace Klacks.Api.Data.Seed
{
    public static class CountryNameCorrectionSql
    {
        public const string CountryTable = "countries";

        private const string UsaId = "276e0392-bfa3-4230-b8a7-8e9fdfecad57";
        private const string French = "fr";
        private const string Italian = "it";

        public static readonly IReadOnlyList<SeededNameCorrection> Corrections =
        [
            new(CountryTable, UsaId, French, "États-Unis d''Amérique", "États-Unis d'Amérique"),
            new(CountryTable, UsaId, Italian, "Stati Uniti d''America", "Stati Uniti d'America"),
        ];
    }
}
