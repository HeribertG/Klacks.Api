// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared SQL building blocks for the data migrations and seeds that write the multilingual jsonb
/// description of seeded calendar rules: the empty seed shape, the "description is still empty" predicate,
/// the JSON serialisation of a text dictionary and literal/id-list quoting.
/// </summary>
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Klacks.Api.Data.Seed
{
    internal static class CalendarRuleDescriptionSql
    {
        public const string SeedEmptyDescription = @"{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}";

        public const string DescriptionIsEmptyPredicate =
            "(CASE WHEN jsonb_typeof(description) = 'object' " +
            "THEN NOT EXISTS (SELECT 1 FROM jsonb_each_text(description) AS d(key, value) WHERE COALESCE(d.value, '') <> '') " +
            "ELSE TRUE END)";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static string DescriptionJson(IReadOnlyDictionary<string, string> texts) =>
            JsonSerializer.Serialize(texts, JsonOptions);

        public static string SqlLiteral(string value) => value.Replace("'", "''");

        public static string IdList(IEnumerable<string> ids) =>
            string.Join(", ", ids.Select(id => $"'{id}'::uuid"));
    }
}
