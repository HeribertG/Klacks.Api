// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs and uninstalls content-related data (docs, skill synonyms, navigation synonyms, sentiment keywords, translations)
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
            var allSkills = await skillRepo.GetAllEnabledAsync();
            var count = 0;

            foreach (var skill in allSkills)
            {
                if (!synonymMap.TryGetValue(skill.Name, out var keywords))
                    continue;

                skill.Synonyms ??= new Dictionary<string, List<string>>();
                skill.Synonyms[code] = keywords;
                await skillRepo.UpdateAsync(skill);
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
        await MergeNonCoreJsonbTranslationsAsync(db, code,
            LanguagePluginConstants.CountriesFileName, "countries", hasDescription: false);
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
            if (MultiLanguage.CoreLanguages.Contains(prop.Name))
                continue;

            var val = prop.Value.GetString();
            if (val != null)
                nonCoreValues[prop.Name] = val;
        }

        if (nonCoreValues.Count == 0)
            return 0;

        var mergeJson = JsonSerializer.Serialize(nonCoreValues);
        var sql = $"UPDATE {tableName} SET {propertyName} = {propertyName} || {{0}}::jsonb WHERE id = {{1}}::uuid";
        await db.Database.ExecuteSqlRawAsync(sql, mergeJson, id);

        return 1;
    }
}
