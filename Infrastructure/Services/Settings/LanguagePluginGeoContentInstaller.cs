// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs and uninstalls the geographic content of language plugins: plugin countries and states,
/// the non-core translations of calendar rules, and the plugin language in the names of the pre-seeded
/// default countries and states.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginGeoContentInstaller
{
    private const string CountryProperty = "country";
    private const string StateProperty = "state";
    private const string NameProperty = "name";
    private const string DescriptionProperty = "description";
    private const string EnglishLanguage = "en";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginGeoContentInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    public async Task MergeNonCoreTranslationsAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        await MergeNonCoreJsonbTranslationsAsync(db, code,
            LanguagePluginConstants.CalendarRulesFileName, "calendar_rule", hasDescription: true);
    }

    /// <summary>
    /// Installs the plugin's home country from {code}/countries.json: inserts it as a new
    /// selectable country if it doesn't exist yet, or merges its name translations into the
    /// existing row otherwise. Unlike calendar rules/states, a plugin country is expected to be a
    /// brand-new row (e.g. installing the "ar" plugin should add Saudi Arabia as a country), so it
    /// needs upsert semantics instead of the update-only merge used for the other content types. A soft-deleted
    /// row is skipped so the startup backfill never writes into or undoes a deletion; reinstalling the pack revives it.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being installed.</param>
    public async Task InstallCountryAsync(IServiceScope scope, string code)
    {
        var filePath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.CountriesFileName);
        if (!File.Exists(filePath))
            return;

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            var json = File.ReadAllText(filePath);
            var entries = JsonSerializer.Deserialize<List<PluginCountryEntry>>(json, JsonOptions);
            if (entries == null || entries.Count == 0)
                return;

            var count = 0;
            foreach (var entry in entries)
            {
                if (!Guid.TryParse(entry.Id, out var id) || string.IsNullOrEmpty(entry.Abbreviation))
                    continue;

                var match = await LanguagePluginGeoRowResolver.FindCountryAsync(db, id, entry.Abbreviation, code, _logger);
                var existing = match.Row;
                if (existing is { IsDeleted: true })
                    continue;

                if (existing != null)
                {
                    foreach (var (language, value) in entry.Name)
                    {
                        if (!string.IsNullOrEmpty(value))
                            existing.Name.SetValue(language, value);
                    }
                }
                else
                {
                    var name = new MultiLanguage();
                    foreach (var (language, value) in entry.Name)
                    {
                        if (!string.IsNullOrEmpty(value))
                            name.SetValue(language, value);
                    }

                    db.Countries.Add(new Countries
                    {
                        Id = match.IdForInsert(id),
                        Abbreviation = entry.Abbreviation,
                        Name = name,
                        Prefix = entry.Prefix
                    });
                }

                count++;
            }

            if (count > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation(
                    "Installed country for language plugin '{Code}': {Count} row(s) upserted",
                    code.ForLog(), count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install country for language plugin '{Code}'", code.ForLog());
        }
    }

    private sealed class PluginCountryEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public Dictionary<string, string> Name { get; set; } = new();
        public string Prefix { get; set; } = string.Empty;
    }

    /// <summary>
    /// Installs the plugin's subdivisions from {code}/states.json: inserts each entry as a new
    /// selectable state if it doesn't exist yet, or merges its name translations into the existing
    /// row otherwise. A plugin country ships its own subdivisions (e.g. the Spanish autonomous
    /// communities that come with the "es" plugin) that are not part of the core seed, so those
    /// rows need upsert semantics instead of the update-only merge used for calendar rules. A soft-deleted row is
    /// skipped so the startup backfill never writes into or undoes a deletion; reinstalling the pack revives it.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being installed.</param>
    public async Task InstallStatesAsync(IServiceScope scope, string code)
    {
        var filePath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.StatesFileName);
        if (!File.Exists(filePath))
            return;

        try
        {
            var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
            var json = File.ReadAllText(filePath);
            var entries = JsonSerializer.Deserialize<List<PluginStateEntry>>(json, JsonOptions);
            if (entries == null || entries.Count == 0)
                return;

            var count = 0;
            foreach (var entry in entries)
            {
                if (!Guid.TryParse(entry.Id, out var id) || string.IsNullOrEmpty(entry.Abbreviation))
                    continue;

                var match = await LanguagePluginGeoRowResolver.FindStateAsync(
                    db, id, entry.CountryPrefix, entry.Abbreviation, code, _logger);
                var existing = match.Row;
                if (existing is { IsDeleted: true })
                    continue;

                if (existing != null)
                {
                    foreach (var (language, value) in entry.Name)
                    {
                        if (!string.IsNullOrEmpty(value))
                            existing.Name.SetValue(language, value);
                    }
                }
                else
                {
                    var name = new MultiLanguage();
                    foreach (var (language, value) in entry.Name)
                    {
                        if (!string.IsNullOrEmpty(value))
                            name.SetValue(language, value);
                    }

                    db.State.Add(new State
                    {
                        Id = match.IdForInsert(id),
                        Abbreviation = entry.Abbreviation,
                        CountryPrefix = entry.CountryPrefix,
                        Name = name
                    });
                }

                count++;
            }

            if (count > 0)
            {
                await db.SaveChangesAsync();
                _logger.LogInformation(
                    "Installed states for language plugin '{Code}': {Count} row(s) upserted",
                    code.ForLog(), count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install states for language plugin '{Code}'", code.ForLog());
        }
    }

    private sealed class PluginStateEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
        public string CountryPrefix { get; set; } = string.Empty;
        public Dictionary<string, string> Name { get; set; } = new();
    }

    /// <summary>
    /// Adds the installed plugin language to the multilingual name of the pre-seeded default
    /// countries and states (CH/DE/AT/FR/IT/LI/USA) whose names ship with the core languages only.
    /// Translations are read from the shared master file that lives in the plugin root directory.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being installed.</param>
    public async Task MergeDefaultGeoTranslationsAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var filePath = Path.Combine(_pluginDirectory, LanguagePluginConstants.DefaultGeoTranslationsFileName);
        if (!File.Exists(filePath))
            return;

        var languageKey = code.ToLowerInvariant();

        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var count = 0;
            count += await MergeSingleLanguageAsync(db, "countries", root, "countries", languageKey);
            count += await MergeSingleLanguageAsync(db, "state", root, "states", languageKey);

            if (count > 0)
            {
                _logger.LogInformation(
                    "Merged default geo translations into {Count} record(s) for language plugin '{Code}'",
                    count, code.ForLog());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to merge default geo translations for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the plugin language key from the multilingual name of all countries and states,
    /// keeping the default entities symmetric with the install step. Core languages are never touched.
    /// </summary>
    /// <param name="scope">Service scope providing the database context.</param>
    /// <param name="code">Plugin language code being uninstalled.</param>
    public async Task RemoveDefaultGeoTranslationsAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var languageKey = code.ToLowerInvariant();
        if (MultiLanguage.CoreLanguages.Contains(languageKey))
            return;

        await db.Database.ExecuteSqlRawAsync("UPDATE state SET name = name - {0}", languageKey);
        await db.Database.ExecuteSqlRawAsync("UPDATE countries SET name = name - {0}", languageKey);
    }

    private static async Task<int> MergeSingleLanguageAsync(
        DataBaseContext db, string tableName, JsonElement root, string arrayProperty, string languageKey)
    {
        if (!root.TryGetProperty(arrayProperty, out var array) || array.ValueKind != JsonValueKind.Array)
            return 0;

        var count = 0;
        foreach (var element in array.EnumerateArray())
        {
            var id = element.GetProperty("id").GetString();
            if (string.IsNullOrEmpty(id))
                continue;

            if (!element.TryGetProperty("name", out var nameObj)
                || !nameObj.TryGetProperty(languageKey, out var valueElement))
                continue;

            var value = valueElement.GetString();
            if (string.IsNullOrEmpty(value))
                continue;

            var mergeJson = JsonSerializer.Serialize(new Dictionary<string, string> { [languageKey] = value });
            var sql = $"UPDATE {tableName} SET name = {{0}}::jsonb || name "
                    + "WHERE id = {1}::uuid AND (name ->> {2}) IS NULL";
            count += await db.Database.ExecuteSqlRawAsync(sql, mergeJson, id, languageKey);
        }

        return count;
    }

    private async Task MergeNonCoreJsonbTranslationsAsync(
        DataBaseContext db, string code, string fileName, string tableName, bool hasDescription)
    {
        var filePath = Path.Combine(_pluginDirectory, code, fileName);
        if (!File.Exists(filePath))
            return;

        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);
            var count = 0;

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var id = element.GetProperty("id").GetString();
                if (string.IsNullOrEmpty(id))
                    continue;

                var country = element.GetProperty(CountryProperty).GetString() ?? string.Empty;
                var state = element.GetProperty(StateProperty).GetString() ?? string.Empty;
                var englishName = element.TryGetProperty(NameProperty, out var names)
                    && names.TryGetProperty(EnglishLanguage, out var english)
                        ? english.GetString() ?? string.Empty
                        : string.Empty;

                var ruleKey = new CalendarRuleKey(id, country, state, englishName);
                count += await MergeJsonbPropertyAsync(db, tableName, ruleKey, element, NameProperty);

                if (hasDescription)
                {
                    await MergeJsonbPropertyAsync(db, tableName, ruleKey, element, DescriptionProperty);
                }
            }

            if (count > 0)
            {
                _logger.LogInformation(
                    "Merged non-core translations for {Count} {Table} record(s) in language plugin '{Code}'",
                    count, tableName.ForLog(), code.ForLog());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to merge non-core translations for {Table} in language plugin '{Code}'",
                tableName.ForLog(), code.ForLog());
        }
    }

    private static async Task<int> MergeJsonbPropertyAsync(
        DataBaseContext db, string tableName, CalendarRuleKey ruleKey, JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propObj))
            return 0;

        var nonCoreValues = new Dictionary<string, string>();

        foreach (var prop in propObj.EnumerateObject())
        {
            var key = prop.Name.ToLowerInvariant();
            if (MultiLanguage.CoreLanguages.Contains(key))
                continue;

            var val = prop.Value.GetString();
            if (val != null)
                nonCoreValues[key] = val;
        }

        if (nonCoreValues.Count == 0)
            return 0;

        var mergeJson = JsonSerializer.Serialize(nonCoreValues);
        var update = $"UPDATE {tableName} SET {propertyName} = {propertyName} || {{0}}::jsonb WHERE country = {{2}} AND state = {{3}} AND ";
        if (string.IsNullOrEmpty(ruleKey.EnglishName))
        {
            return await db.Database.ExecuteSqlRawAsync(
                update + "id = {1}::uuid", mergeJson, ruleKey.Id, ruleKey.Country, ruleKey.State);
        }

        return await db.Database.ExecuteSqlRawAsync(
            update + $"(id = {{1}}::uuid OR {NameProperty} ->> '{EnglishLanguage}' = {{4}})",
            mergeJson, ruleKey.Id, ruleKey.Country, ruleKey.State, ruleKey.EnglishName);
    }

    private sealed record CalendarRuleKey(string Id, string Country, string State, string EnglishName);
}
