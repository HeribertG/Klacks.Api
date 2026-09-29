// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single source of the SQL that gives the seeded non-statutory calendar rules (is_mandatory = false)
/// their explanatory description. Consumed by the fresh-install seed (after both calendar rule seeds,
/// so the rows exist) and by the migration that backfills existing databases.
/// </summary>
/// <remarks>
/// Rows are addressed by their fixed seed id, never by name. A description is only written while it is
/// still empty (NULL, a non-object, or an object whose values are all empty strings - the seed shape
/// and the shape MultiLanguage serialises an empty value to), so an administrator's own text is never
/// overwritten. Remove reverts only rows that still carry exactly the text Apply wrote.
/// </remarks>
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class UnofficialHolidayDescriptionsSql
    {
        private const string SeedEmptyDescription = @"{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}";

        private const string DescriptionIsEmptyPredicate =
            "(CASE WHEN jsonb_typeof(description) = 'object' " +
            "THEN NOT EXISTS (SELECT 1 FROM jsonb_each_text(description) AS d(key, value) WHERE COALESCE(d.value, '') <> '') " +
            "ELSE TRUE END)";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static readonly IReadOnlyList<string> SwissEveIds =
        [
            .. Enumerable.Range(1, 26).Select(i => $"0ab12401-0001-0001-0001-{i:D12}"),
            .. Enumerable.Range(1, 25).Select(i => $"01231001-0001-0001-0001-{i:D12}"),
        ];

        public static readonly IReadOnlyList<(IReadOnlyDictionary<string, string> Texts, IReadOnlyList<string> Ids)> Assignments =
        [
            (UnofficialHolidayDescriptionTexts.EveHalfWorkday,
            [
                .. SwissEveIds,
                "a0000001-0001-0001-0001-000000000015",
                "a0000001-0001-0001-0001-000000000016",
                "de000001-0001-0001-0001-000000000010",
                "de000001-0001-0001-0001-000000000011",
            ]),
            (UnofficialHolidayDescriptionTexts.EveNormalWorkday,
            [
                "10000001-0001-0001-0001-000000000013",
                "10000001-0001-0001-0001-000000000014",
                "f0000001-0001-0001-0001-000000000012",
                "f0000001-0001-0001-0001-000000000013",
            ]),
            (UnofficialHolidayDescriptionTexts.AlwaysOnSunday,
            [
                "611f688c-8f58-4e8c-a3a6-a6c54f40f874",
            ]),
            (UnofficialHolidayDescriptionTexts.SomeMunicipalitiesOnly,
            [
                "00319001-0001-0001-0001-000000000001",
                "00319001-0001-0001-0001-000000000002",
                "00319001-0001-0001-0001-000000000004",
                "00319001-0001-0001-0001-000000000009",
                "00629001-0001-0001-0001-000000000001",
            ]),
            (UnofficialHolidayDescriptionTexts.AustrianGoodFriday,
            [
                "a0000001-0001-0001-0001-000000000014",
            ]),
            (UnofficialHolidayDescriptionTexts.RegionalHoliday,
            [
                "a00b0001-0001-0001-0001-000000000001",
                "a00b0001-0001-0001-0001-000000000002",
                "a00b0001-0001-0001-0001-000000000003",
                "a00b0001-0001-0001-0001-000000000004",
                "a00c0001-0001-0001-0001-000000000001",
                "a00d0001-0001-0001-0001-000000000001",
                "a00e0001-0001-0001-0001-000000000001",
                "a00f0001-0001-0001-0001-000000000001",
                "a00a0001-0001-0001-0001-000000000001",
                "a00a0001-0001-0001-0001-000000000002",
                "100bb001-0001-0001-0001-000000000001",
            ]),
            (UnofficialHolidayDescriptionTexts.UsFederalHoliday,
            [
                "05a00001-0001-0001-0001-000000000002",
                "05a00001-0001-0001-0001-000000000003",
                "05a00001-0001-0001-0001-000000000004",
                "05a00001-0001-0001-0001-000000000006",
                "05a00001-0001-0001-0001-000000000007",
                "05a00001-0001-0001-0001-000000000008",
            ]),
        ];

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            foreach (var (texts, ids) in Assignments)
            {
                migrationBuilder.Sql(
                    $"UPDATE public.calendar_rule SET description = '{SqlLiteral(DescriptionJson(texts))}'::jsonb " +
                    $"WHERE id IN ({IdList(ids)}) AND {DescriptionIsEmptyPredicate};");
            }
        }

        public static void Remove(MigrationBuilder migrationBuilder)
        {
            foreach (var (texts, ids) in Assignments)
            {
                migrationBuilder.Sql(
                    $"UPDATE public.calendar_rule SET description = '{SeedEmptyDescription}'::jsonb " +
                    $"WHERE id IN ({IdList(ids)}) AND description = '{SqlLiteral(DescriptionJson(texts))}'::jsonb;");
            }
        }

        public static string DescriptionJson(IReadOnlyDictionary<string, string> texts) =>
            JsonSerializer.Serialize(texts, JsonOptions);

        private static string SqlLiteral(string value) => value.Replace("'", "''");

        private static string IdList(IEnumerable<string> ids) =>
            string.Join(", ", ids.Select(id => $"'{id}'::uuid"));
    }
}
