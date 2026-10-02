// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Adds the installed plugin language to the multilingual name of the pre-seeded holiday rules (CalendarRulesSeed,
/// AdditionalCalendarRulesSeed: CH incl. cantons, DE, AT, FR, IT, LI, GB, USA), which ship with the core languages
/// only. The translations come from the shared master file default-calendar-rule-translations.json in the plugin
/// root: one entry per holiday name, listing the fixed seed ids that carry it. A name that already carries the
/// language (a non-empty value) is never overwritten, so customer edits survive every startup backfill; on uninstall only the names
/// that still equal the pack value are removed.
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

public class LanguagePluginCalendarRuleNameInstaller
{
    private const string CalendarRulesProperty = "calendarRules";
    private const string IdsProperty = "ids";
    private const string NameProperty = "name";

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginCalendarRuleNameInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Writes the pack language into every seeded holiday rule that does not carry it yet. Idempotent, so it
    /// serves both the pack install and the startup backfill of already-installed packs.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being installed.</param>
    public async Task MergeDefaultCalendarRuleTranslationsAsync(IServiceScope scope, string code)
    {
        var languageKey = code.ToLowerInvariant();
        if (MultiLanguage.CoreLanguages.Contains(languageKey))
            return;

        try
        {
            var translations = ReadTranslations(languageKey);
            if (translations.Count == 0)
                return;

            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            var count = await db.Database.ExecuteSqlRawAsync(
                "UPDATE calendar_rule c SET name = c.name || jsonb_build_object({0}::text, t.value) "
                + "FROM jsonb_each_text({1}::jsonb) AS t(key, value) "
                + "WHERE c.id = t.key::uuid AND COALESCE(c.name ->> {0}::text, '') = ''",
                languageKey, JsonSerializer.Serialize(translations));

            if (count > 0)
            {
                _logger.LogInformation(
                    "Merged default holiday name translations into {Count} calendar rule(s) for language plugin '{Code}'",
                    count, code.ForLog());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to merge default holiday name translations for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the pack language from the seeded holiday rules whose name still equals the pack value. A name a
    /// customer typed or changed in that language is left alone, and core languages are never touched.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being uninstalled.</param>
    public async Task RemoveDefaultCalendarRuleTranslationsAsync(IServiceScope scope, string code)
    {
        var languageKey = code.ToLowerInvariant();
        if (MultiLanguage.CoreLanguages.Contains(languageKey))
            return;

        try
        {
            var translations = ReadTranslations(languageKey);
            if (translations.Count == 0)
                return;

            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE calendar_rule c SET name = c.name - {0}::text "
                + "FROM jsonb_each_text({1}::jsonb) AS t(key, value) "
                + "WHERE c.id = t.key::uuid AND (c.name ->> {0}::text) = t.value",
                languageKey, JsonSerializer.Serialize(translations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove default holiday name translations for language plugin '{Code}'", code.ForLog());
        }
    }

    private Dictionary<string, string> ReadTranslations(string languageKey)
    {
        var translations = new Dictionary<string, string>();
        var filePath = Path.Combine(_pluginDirectory, LanguagePluginConstants.DefaultCalendarRuleTranslationsFileName);
        if (!File.Exists(filePath))
            return translations;

        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
        if (!doc.RootElement.TryGetProperty(CalendarRulesProperty, out var array) || array.ValueKind != JsonValueKind.Array)
            return translations;

        foreach (var element in array.EnumerateArray())
        {
            if (!element.TryGetProperty(IdsProperty, out var ids)
                || ids.ValueKind != JsonValueKind.Array
                || !element.TryGetProperty(NameProperty, out var nameObject)
                || nameObject.ValueKind != JsonValueKind.Object)
                continue;

            var value = FindValue(nameObject, languageKey);
            if (string.IsNullOrWhiteSpace(value))
                continue;

            foreach (var idElement in ids.EnumerateArray())
            {
                if (Guid.TryParse(idElement.GetString(), out var id))
                    translations[id.ToString()] = value;
            }
        }

        return translations;
    }

    private static string? FindValue(JsonElement nameObject, string languageKey)
    {
        foreach (var property in nameObject.EnumerateObject())
        {
            if (string.Equals(property.Name, languageKey, StringComparison.OrdinalIgnoreCase))
                return property.Value.GetString();
        }

        return null;
    }
}
