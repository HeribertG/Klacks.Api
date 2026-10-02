// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Adds the installed plugin language to the multilingual name of the pre-seeded default qualifications
/// (QualificationsSeed), which ship with the core languages only. The translations come from the shared master
/// file default-qualification-translations.json in the plugin root, keyed by the fixed seed id. A name that
/// already carries the language is never overwritten, so customer edits survive every startup backfill; on
/// uninstall only the names that still equal the pack value are removed.
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

public class LanguagePluginQualificationInstaller
{
    private const string QualificationsProperty = "qualifications";
    private const string IdProperty = "id";
    private const string NameProperty = "name";

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginQualificationInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Writes the pack language into every default qualification that does not carry it yet. Idempotent, so it
    /// serves both the pack install and the startup backfill of already-installed packs.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being installed.</param>
    public async Task MergeDefaultQualificationTranslationsAsync(IServiceScope scope, string code)
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
                "UPDATE qualification q SET name = jsonb_build_object({0}::text, t.value) || q.name "
                + "FROM jsonb_each_text({1}::jsonb) AS t(key, value) "
                + "WHERE q.id = t.key::uuid AND (q.name ->> {0}::text) IS NULL",
                languageKey, JsonSerializer.Serialize(translations));

            if (count > 0)
            {
                _logger.LogInformation(
                    "Merged default qualification translations into {Count} record(s) for language plugin '{Code}'",
                    count, code.ForLog());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to merge default qualification translations for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the pack language from the default qualifications whose name still equals the pack value. A name
    /// a customer typed or changed in that language is left alone, and core languages are never touched.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being uninstalled.</param>
    public async Task RemoveDefaultQualificationTranslationsAsync(IServiceScope scope, string code)
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
                "UPDATE qualification q SET name = q.name - {0}::text "
                + "FROM jsonb_each_text({1}::jsonb) AS t(key, value) "
                + "WHERE q.id = t.key::uuid AND (q.name ->> {0}::text) = t.value",
                languageKey, JsonSerializer.Serialize(translations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to remove default qualification translations for language plugin '{Code}'", code.ForLog());
        }
    }

    private Dictionary<string, string> ReadTranslations(string languageKey)
    {
        var translations = new Dictionary<string, string>();
        var filePath = Path.Combine(_pluginDirectory, LanguagePluginConstants.DefaultQualificationTranslationsFileName);
        if (!File.Exists(filePath))
            return translations;

        using var doc = JsonDocument.Parse(File.ReadAllText(filePath));
        if (!doc.RootElement.TryGetProperty(QualificationsProperty, out var array) || array.ValueKind != JsonValueKind.Array)
            return translations;

        foreach (var element in array.EnumerateArray())
        {
            if (!element.TryGetProperty(IdProperty, out var idElement)
                || !Guid.TryParse(idElement.GetString(), out var id)
                || !element.TryGetProperty(NameProperty, out var nameObject)
                || nameObject.ValueKind != JsonValueKind.Object)
                continue;

            foreach (var property in nameObject.EnumerateObject())
            {
                if (!string.Equals(property.Name, languageKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = property.Value.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    translations[id.ToString()] = value;
            }
        }

        return translations;
    }
}
