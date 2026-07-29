// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs and uninstalls content-related data (docs, skill synonyms, recipe synonyms, navigation synonyms, sentiment keywords, translations)
/// for language plugins.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginContentInstaller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginContentInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    public async Task InstallDocsAsync(IServiceScope scope, string code)
    {
        var docsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.DocsDirectory);
        if (!Directory.Exists(docsPath))
            return;

        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var htmlFiles = Directory.GetFiles(docsPath, "*.html");
        var count = 0;

        foreach (var filePath in htmlFiles)
        {
            var manualName = Path.GetFileNameWithoutExtension(filePath);
            var htmlContent = await File.ReadAllTextAsync(filePath);

            var existing = await db.PluginDocs
                .FirstOrDefaultAsync(d => d.PluginCode == code && d.ManualName == manualName);

            if (existing != null)
            {
                existing.HtmlContent = htmlContent;
            }
            else
            {
                db.PluginDocs.Add(new PluginDoc
                {
                    Id = Guid.NewGuid(),
                    PluginCode = code,
                    ManualName = manualName,
                    HtmlContent = htmlContent
                });
            }

            count++;
        }

        _logger.LogInformation("Installed {Count} doc(s) for language plugin '{Code}'", count, code.ForLog());
    }

    public async Task UninstallDocsAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var docs = await db.PluginDocs
            .Where(d => d.PluginCode == code)
            .ToListAsync();

        db.PluginDocs.RemoveRange(docs);
        _logger.LogInformation("Uninstalled {Count} doc(s) for language plugin '{Code}'", docs.Count, code.ForLog());
    }

    public async Task InstallSkillSynonymsAsync(IServiceScope scope, string code)
    {
        var synonymsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.SkillSynonymsFileName);
        if (!File.Exists(synonymsPath))
            return;

        try
        {
            var json = File.ReadAllText(synonymsPath);
            var synonymMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (synonymMap == null || synonymMap.Count == 0)
                return;

            var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
            var phraseRepo = scope.ServiceProvider.GetRequiredService<ISkillPhraseRepository>();
            var allSkills = await skillRepo.GetAllEnabledAsync();
            var count = 0;

            foreach (var skill in allSkills)
            {
                if (!synonymMap.TryGetValue(skill.Name, out var keywords))
                    continue;

                skill.Synonyms ??= new Dictionary<string, List<string>>();
                skill.Synonyms[code] = keywords;
                await skillRepo.UpdateAsync(skill);
                await ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Skill, skill.Name, code, keywords);
                count++;
            }

            _logger.LogInformation(
                "Installed skill synonyms for language plugin '{Code}': {Count} skill(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install skill synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    public async Task UninstallSkillSynonymsAsync(IServiceScope scope, string code)
    {
        var synonymsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.SkillSynonymsFileName);
        if (!File.Exists(synonymsPath))
            return;

        try
        {
            var json = File.ReadAllText(synonymsPath);
            var synonymMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (synonymMap == null || synonymMap.Count == 0)
                return;

            var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
            var phraseRepo = scope.ServiceProvider.GetRequiredService<ISkillPhraseRepository>();
            var allSkills = await skillRepo.GetAllEnabledAsync();
            var count = 0;

            foreach (var skill in allSkills)
            {
                if (!synonymMap.ContainsKey(skill.Name))
                    continue;

                if (skill.Synonyms == null || !skill.Synonyms.ContainsKey(code))
                    continue;

                skill.Synonyms.Remove(code);
                await skillRepo.UpdateAsync(skill);
                await ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Skill, skill.Name, code, []);
                count++;
            }

            _logger.LogInformation(
                "Uninstalled skill synonyms for language plugin '{Code}': {Count} skill(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall skill synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    public async Task InstallRecipeSynonymsAsync(IServiceScope scope, string code)
    {
        var synonymsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.RecipeSynonymsFileName);
        if (!File.Exists(synonymsPath))
            return;

        try
        {
            var json = File.ReadAllText(synonymsPath);
            var synonymMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (synonymMap == null || synonymMap.Count == 0)
                return;

            var recipeRepo = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
            var phraseRepo = scope.ServiceProvider.GetRequiredService<ISkillPhraseRepository>();
            var allRecipes = await recipeRepo.GetAllEnabledAsync();
            var count = 0;

            foreach (var recipe in allRecipes)
            {
                if (!synonymMap.TryGetValue(recipe.Name, out var keywords))
                    continue;

                recipe.Synonyms ??= new Dictionary<string, List<string>>();
                recipe.Synonyms[code] = keywords;
                await recipeRepo.UpdateAsync(recipe);
                await ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Recipe, recipe.Name, code, keywords);
                count++;
            }

            _logger.LogInformation(
                "Installed recipe synonyms for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install recipe synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    public async Task UninstallRecipeSynonymsAsync(IServiceScope scope, string code)
    {
        var synonymsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.RecipeSynonymsFileName);
        if (!File.Exists(synonymsPath))
            return;

        try
        {
            var json = File.ReadAllText(synonymsPath);
            var synonymMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (synonymMap == null || synonymMap.Count == 0)
                return;

            var recipeRepo = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
            var phraseRepo = scope.ServiceProvider.GetRequiredService<ISkillPhraseRepository>();
            var allRecipes = await recipeRepo.GetAllEnabledAsync();
            var count = 0;

            foreach (var recipe in allRecipes)
            {
                if (!synonymMap.ContainsKey(recipe.Name))
                    continue;

                if (recipe.Synonyms == null || !recipe.Synonyms.ContainsKey(code))
                    continue;

                recipe.Synonyms.Remove(code);
                await recipeRepo.UpdateAsync(recipe);
                await ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Recipe, recipe.Name, code, []);
                count++;
            }

            _logger.LogInformation(
                "Uninstalled recipe synonyms for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall recipe synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Writes the synonyms a language pack contributes into skill_phrase next to the legacy jsonb
    /// dictionary. The replacement is restricted to the LanguagePack origin of exactly this language
    /// code, so installing or removing a pack can neither delete the seeded core-language phrases nor
    /// those of another installed pack. The code is used verbatim as the language key, because the
    /// jsonb dictionary is keyed the same way and the two must stay comparable.
    /// </summary>
    /// <param name="phraseRepo">Repository writing the skill_phrase rows</param>
    /// <param name="ownerKind">Skill or Recipe, see SkillPhraseOwnerKinds</param>
    /// <param name="ownerName">Business name of the skill or recipe</param>
    /// <param name="code">Language code of the plugin being installed or uninstalled</param>
    /// <param name="synonyms">The synonyms of that language; an empty list removes them</param>
    private static async Task ReplacePackPhrasesAsync(
        ISkillPhraseRepository phraseRepo,
        string ownerKind,
        string ownerName,
        string code,
        IReadOnlyList<string> synonyms)
    {
        await phraseRepo.ReplaceForLanguageAsync(
            ownerKind,
            ownerName,
            SkillPhraseKinds.Synonym,
            SkillPhraseSources.LanguagePack,
            code,
            synonyms);
    }

    public async Task InstallSentimentKeywordsAsync(IServiceScope scope, string code)
    {
        var keywordsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.SentimentKeywordsFileName);
        if (!File.Exists(keywordsPath))
            return;

        try
        {
            var json = File.ReadAllText(keywordsPath);
            var keywordMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (keywordMap == null || keywordMap.Count == 0)
                return;

            var sentimentRepo = scope.ServiceProvider.GetRequiredService<ISentimentKeywordRepository>();
            await sentimentRepo.UpsertAsync(code, keywordMap, SentimentKeywordSources.Plugin);

            scope.ServiceProvider.GetRequiredService<ISentimentAnalyzer>().ReloadKeywords();

            _logger.LogInformation(
                "Installed sentiment keywords for language plugin '{Code}'", code.ForLog());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install sentiment keywords for language plugin '{Code}'", code.ForLog());
        }
    }

    public async Task UninstallSentimentKeywordsAsync(IServiceScope scope, string code)
    {
        var keywordsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.SentimentKeywordsFileName);
        if (!File.Exists(keywordsPath))
            return;

        try
        {
            var sentimentRepo = scope.ServiceProvider.GetRequiredService<ISentimentKeywordRepository>();
            await sentimentRepo.DeleteByLanguageAsync(code);

            scope.ServiceProvider.GetRequiredService<ISentimentAnalyzer>().ReloadKeywords();

            _logger.LogInformation(
                "Uninstalled sentiment keywords for language plugin '{Code}'", code.ForLog());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall sentiment keywords for language plugin '{Code}'", code.ForLog());
        }
    }

    public Task InstallWakeWordsAsync(string code)
    {
        var wakeWordsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.WakeWordsFileName);
        if (File.Exists(wakeWordsPath))
            _logger.LogInformation("Wake-words file registered for language plugin '{Code}'", code.ForLog());
        else
            _logger.LogWarning("Wake-words file not found for language plugin '{Code}' at '{Path}'", code.ForLog(), wakeWordsPath.ForLog());

        return Task.CompletedTask;
    }

    public async Task InstallNavigationSynonymsAsync(IServiceScope scope, string code)
    {
        var navTargetsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.NavigationTargetsFileName);
        if (!File.Exists(navTargetsPath))
            return;

        try
        {
            var json = File.ReadAllText(navTargetsPath);
            var overlay = JsonSerializer.Deserialize<Dictionary<string, PluginNavigationEntry>>(json, JsonOptions);
            if (overlay == null || overlay.Count == 0)
                return;

            var synonymRepo = scope.ServiceProvider.GetRequiredService<INavigationTargetSynonymRepository>();
            var count = 0;

            foreach (var (targetId, entry) in overlay)
            {
                if (entry.Synonyms == null || entry.Synonyms.Length == 0)
                    continue;

                await synonymRepo.ReplaceForTargetLanguageAsync(targetId, code, entry.Synonyms, SynonymSources.Plugin);
                count++;
            }

            _logger.LogInformation(
                "Installed navigation synonyms for language plugin '{Code}': {Count} target(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install navigation synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    public async Task UninstallNavigationSynonymsAsync(IServiceScope scope, string code)
    {
        var navTargetsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.NavigationTargetsFileName);
        if (!File.Exists(navTargetsPath))
            return;

        try
        {
            var json = File.ReadAllText(navTargetsPath);
            var overlay = JsonSerializer.Deserialize<Dictionary<string, PluginNavigationEntry>>(json, JsonOptions);
            if (overlay == null || overlay.Count == 0)
                return;

            var synonymRepo = scope.ServiceProvider.GetRequiredService<INavigationTargetSynonymRepository>();
            var count = 0;

            foreach (var targetId in overlay.Keys)
            {
                await synonymRepo.ReplaceForTargetLanguageAsync(targetId, code, [], SynonymSources.Plugin);
                count++;
            }

            _logger.LogInformation(
                "Uninstalled navigation synonyms for language plugin '{Code}': {Count} target(s) cleared",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall navigation synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    private sealed class PluginNavigationEntry
    {
        public string[] Synonyms { get; set; } = [];
        public string Status { get; set; } = "generated";
    }

    public async Task MergeNonCoreTranslationsAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        await MergeNonCoreJsonbTranslationsAsync(db, code,
            LanguagePluginConstants.CalendarRulesFileName, "calendar_rule", hasDescription: true);
        await MergeNonCoreJsonbTranslationsAsync(db, code,
            LanguagePluginConstants.StatesFileName, "state", hasDescription: false);
    }

    /// <summary>
    /// Installs the plugin's home country from {code}/countries.json: inserts it as a new
    /// selectable country if it doesn't exist yet, or merges its name translations into the
    /// existing row otherwise. Unlike calendar rules/states, a plugin country is expected to be a
    /// brand-new row (e.g. installing the "ar" plugin should add Saudi Arabia as a country), so it
    /// needs upsert semantics instead of the update-only merge used for the other content types.
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

                var existing = await db.Countries.FirstOrDefaultAsync(c => c.Id == id);

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
                        Id = id,
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

                count += await MergeJsonbPropertyAsync(db, tableName, id, element, "name");

                if (hasDescription)
                {
                    await MergeJsonbPropertyAsync(db, tableName, id, element, "description");
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
        DataBaseContext db, string tableName, string id, JsonElement element, string propertyName)
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
        var sql = $"UPDATE {tableName} SET {propertyName} = {propertyName} || {{0}}::jsonb WHERE id = {{1}}::uuid";
        await db.Database.ExecuteSqlRawAsync(sql, mergeJson, id);

        return 1;
    }
}
