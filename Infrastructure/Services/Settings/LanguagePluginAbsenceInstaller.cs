// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Adds the installed plugin language to the multilingual name and abbreviation of the pre-seeded default
/// absence types (AbsencesSeed plus the on-call and unpaid-training migrations), which ship with the core
/// languages only. The translations come from the shared master file default-absence-translations.json in the
/// plugin root, keyed by the fixed seed id. A row is only filled while its German name still equals the seed
/// name, so a seed row a customer repurposed is left alone; a field that already carries the language (a
/// non-empty value) is never overwritten; an abbreviation is skipped when another live absence type already
/// uses it in that language (or shows it there through the core-language fallback of the UI), because
/// abbreviations are the input shortcuts of the schedule cells. On uninstall
/// only the values that still equal the pack value are removed.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginAbsenceInstaller
{
    internal const string AbsencesProperty = "absences";
    internal const string IdProperty = "id";
    internal const string SeedNameProperty = "de";
    internal const string NameProperty = "name";
    internal const string AbbreviationProperty = "abbreviation";
    internal const string ValueProperty = "value";

    private const string MergeNameSql =
        "UPDATE absence a SET name = a.name || jsonb_build_object({0}::text, t.value ->> {2}::text) "
        + "FROM jsonb_each({1}::jsonb) AS t(key, value) "
        + "WHERE a.id = t.key::uuid "
        + "AND (a.name ->> {3}::text) = (t.value ->> {3}::text) "
        + "AND COALESCE(a.name ->> {0}::text, '') = ''";

    private static readonly string ResolvedOtherAbbreviationSql =
        "COALESCE(o.abbreviation ->> {0}::text, "
        + string.Join(", ", MultiLanguage.CoreLanguages.Select(language => $"o.abbreviation ->> '{language}'"))
        + ")";

    private static readonly string MergeAbbreviationSql =
        "UPDATE absence a SET abbreviation = a.abbreviation || jsonb_build_object({0}::text, t.value ->> {2}::text) "
        + "FROM jsonb_each({1}::jsonb) AS t(key, value) "
        + "WHERE a.id = t.key::uuid "
        + "AND (a.name ->> {3}::text) = (t.value ->> {3}::text) "
        + "AND COALESCE(a.abbreviation ->> {0}::text, '') = '' "
        + "AND NOT EXISTS (SELECT 1 FROM absence o WHERE o.id <> a.id AND o.is_deleted = false "
        + "AND upper(" + ResolvedOtherAbbreviationSql + ") = upper(t.value ->> {2}::text))";

    private const string RemoveNameSql =
        "UPDATE absence a SET name = a.name - {0}::text "
        + "FROM jsonb_each({1}::jsonb) AS t(key, value) "
        + "WHERE a.id = t.key::uuid AND (a.name ->> {0}::text) = (t.value ->> {2}::text)";

    private const string RemoveAbbreviationSql =
        "UPDATE absence a SET abbreviation = a.abbreviation - {0}::text "
        + "FROM jsonb_each({1}::jsonb) AS t(key, value) "
        + "WHERE a.id = t.key::uuid AND (a.abbreviation ->> {0}::text) = (t.value ->> {2}::text)";

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginAbsenceInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Writes the pack language into the name and abbreviation of every default absence type that does not carry
    /// it yet and is still the seed row. Idempotent, so it serves both the pack install and the startup backfill
    /// of already-installed packs.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being installed.</param>
    public async Task MergeDefaultAbsenceTranslationsAsync(IServiceScope scope, string code)
    {
        var languageKey = code.ToLowerInvariant();
        if (MultiLanguage.CoreLanguages.Contains(languageKey))
            return;

        try
        {
            var names = ReadTranslations(languageKey, NameProperty);
            var abbreviations = ReadTranslations(languageKey, AbbreviationProperty);
            if (names.Count == 0 && abbreviations.Count == 0)
                return;

            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            var nameCount = await db.Database.ExecuteSqlRawAsync(
                MergeNameSql, languageKey, JsonSerializer.Serialize(names), ValueProperty, SeedNameProperty);
            var abbreviationCount = await db.Database.ExecuteSqlRawAsync(
                MergeAbbreviationSql, languageKey, JsonSerializer.Serialize(abbreviations), ValueProperty, SeedNameProperty);

            if (nameCount > 0 || abbreviationCount > 0)
            {
                _logger.LogInformation(
                    "Merged default absence translations ({NameCount} name(s), {AbbreviationCount} abbreviation(s)) for language plugin '{Code}'",
                    nameCount, abbreviationCount, code.ForLog());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to merge default absence translations for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the pack language from the name and abbreviation of the default absence types whose value still
    /// equals the pack value. A value a customer typed or changed in that language is left alone, and core
    /// languages are never touched.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being uninstalled.</param>
    public async Task RemoveDefaultAbsenceTranslationsAsync(IServiceScope scope, string code)
    {
        var languageKey = code.ToLowerInvariant();
        if (MultiLanguage.CoreLanguages.Contains(languageKey))
            return;

        try
        {
            var names = ReadTranslations(languageKey, NameProperty);
            var abbreviations = ReadTranslations(languageKey, AbbreviationProperty);
            if (names.Count == 0 && abbreviations.Count == 0)
                return;

            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            await db.Database.ExecuteSqlRawAsync(
                RemoveNameSql, languageKey, JsonSerializer.Serialize(names), ValueProperty);
            await db.Database.ExecuteSqlRawAsync(
                RemoveAbbreviationSql, languageKey, JsonSerializer.Serialize(abbreviations), ValueProperty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove default absence translations for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Reads one field of the master file for one language: seed id to the pair of seed German name and pack
    /// value. Entries without an id, a German seed name or a non-blank value in the language are skipped.
    /// </summary>
    /// <param name="languageKey">Lower-case plugin language code.</param>
    /// <param name="field">Master file field, either the name or the abbreviation property.</param>
    internal Dictionary<string, Dictionary<string, string>> ReadTranslations(string languageKey, string field)
    {
        var translations = new Dictionary<string, Dictionary<string, string>>();
        var filePath = Path.Combine(_pluginDirectory, LanguagePluginConstants.DefaultAbsenceTranslationsFileName);
        if (!File.Exists(filePath))
            return translations;

        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
        if (!doc.RootElement.TryGetProperty(AbsencesProperty, out var array) || array.ValueKind != JsonValueKind.Array)
            return translations;

        foreach (var element in array.EnumerateArray())
        {
            if (!element.TryGetProperty(IdProperty, out var idElement)
                || !Guid.TryParse(idElement.GetString(), out var id)
                || !element.TryGetProperty(SeedNameProperty, out var seedNameElement)
                || seedNameElement.ValueKind != JsonValueKind.String
                || !element.TryGetProperty(field, out var valueObject)
                || valueObject.ValueKind != JsonValueKind.Object)
                continue;

            var seedName = seedNameElement.GetString();
            var value = FindValue(valueObject, languageKey);
            if (string.IsNullOrWhiteSpace(seedName) || string.IsNullOrWhiteSpace(value))
                continue;

            translations[id.ToString()] = new Dictionary<string, string>
            {
                [SeedNameProperty] = seedName,
                [ValueProperty] = value
            };
        }

        return translations;
    }

    private static string? FindValue(JsonElement valueObject, string languageKey)
    {
        foreach (var property in valueObject.EnumerateObject())
        {
            if (string.Equals(property.Name, languageKey, StringComparison.OrdinalIgnoreCase))
                return property.Value.GetString();
        }

        return null;
    }
}
