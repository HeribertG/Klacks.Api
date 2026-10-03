// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Corrections for the name of the seeded Berchtoldstag rules (2 January, 13 Swiss cantons). Older builds named the
/// day after a "Saint Berchtold" in English and in the plugin languages ("St. Berchtold's Day", "Sankt Berchtolds
/// dag", "聖ベルヒトルトの日" ...); no such saint is behind the day, so these are wrong translations of the proper name.
/// German, French ("Saint-Berchtold", the term of the federal personnel ordinance) and Italian ("San Bertoldo",
/// customary in Ticino) are not corrected. CalendarRulesSeed inserts once and the plugin language merge only fills
/// empty values, so existing databases never receive the corrected texts; the corrections are applied by the
/// BackfillSeededBerchtoldstagNames migration. The faulty and corrected texts are frozen here on purpose: a
/// migration is history.
/// </summary>
namespace Klacks.Api.Data.Seed
{
    public static class CalendarRuleNameCorrectionSql
    {
        public const string CalendarRuleTable = "calendar_rule";

        public static readonly IReadOnlyList<string> BerchtoldstagRuleIds =
        [
            "b0102001-0001-0001-0001-000000000001",
            "b0102001-0001-0001-0001-000000000002",
            "b0102001-0001-0001-0001-000000000003",
            "b0102001-0001-0001-0001-000000000004",
            "b0102001-0001-0001-0001-000000000005",
            "b0102001-0001-0001-0001-000000000006",
            "b0102001-0001-0001-0001-000000000007",
            "b0102001-0001-0001-0001-000000000008",
            "b0102001-0001-0001-0001-000000000009",
            "b0102001-0001-0001-0001-000000000010",
            "b0102001-0001-0001-0001-000000000011",
            "b0102001-0001-0001-0001-000000000012",
            "b0102001-0001-0001-0001-000000000013",
        ];

        public static readonly IReadOnlyList<(string Language, string FaultyName, string CorrectedName)> BerchtoldstagNames =
        [
            ("en", "St. Berchtold's Day", "Berchtold's Day"),
            ("ar", "عيد القديس بيرشتولد", "يوم بيرشتولد"),
            ("cs", "Den sv. Berchtolda", "Berchtoldův den"),
            ("da", "Sankt Berchtolds dag", "Berchtolds dag"),
            ("el", "Ημέρα του Αγίου Μπέρχτολντ", "Ημέρα του Μπέρχτολντ"),
            ("es", "Día de San Bertoldo", "Día de Berchtold"),
            ("he", "יום ברכטולד הקדוש", "יום ברכטולד"),
            ("id", "Hari Santo Berchtold", "Hari Berchtold"),
            ("ja", "聖ベルヒトルトの日", "ベルヒトルトの日"),
            ("ko", "성 베르히톨트의 날", "베르히톨트의 날"),
            ("ms", "Hari Santo Berchtold", "Hari Berchtold"),
            ("nb", "Sankt Berchtolds dag", "Berchtolds dag"),
            ("nl", "Sint-Berchtoldsdag", "Berchtoldsdag"),
            ("pl", "Dzień św. Berchtolda", "Dzień Berchtolda"),
            ("pt", "Dia de São Bertoldo", "Dia de Berchtold"),
            ("ro", "Ziua Sfântului Berchtold", "Ziua lui Berchtold"),
            ("sv", "Sankt Berchtolds dag", "Berchtolds dag"),
            ("th", "วันนักบุญแบร์ชโทลด์", "วันแบร์ชโทลด์"),
            ("vi", "Ngày Thánh Berchtold", "Ngày Berchtold"),
            ("zh-cn", "圣贝希托尔德节", "贝希托尔德节"),
            ("zh-tw", "聖貝希托爾德節", "貝希托爾德節"),
        ];

        public static readonly IReadOnlyList<SeededNameCorrection> Corrections =
            BerchtoldstagRuleIds
                .SelectMany(id => BerchtoldstagNames.Select(name =>
                    new SeededNameCorrection(CalendarRuleTable, id, name.Language, name.FaultyName, name.CorrectedName)))
                .ToList();
    }
}
