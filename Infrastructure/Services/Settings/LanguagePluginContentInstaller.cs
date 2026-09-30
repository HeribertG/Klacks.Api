// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs and uninstalls content-related data (docs, skill synonyms, recipe synonyms, navigation synonyms,
/// sentiment keywords) for language plugins. The recipe vetoes and anchors live in
/// LanguagePluginRecipeVocabularyInstaller, the countries, states and geo translations in
/// LanguagePluginGeoContentInstaller.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Klacksy;
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

    /// <summary>
    /// Writes the pack's skill synonyms into the enabled skills it names.
    /// </summary>
    /// <param name="scope">Scope providing the skill and phrase repositories</param>
    /// <param name="code">Language code of the pack</param>
    /// <param name="onlySkillNames">When set, only these skills are updated; null updates every enabled skill</param>
    public async Task InstallSkillSynonymsAsync(
        IServiceScope scope, string code, IReadOnlyCollection<string>? onlySkillNames = null)
    {
        var skillFilter = onlySkillNames == null
            ? null
            : new HashSet<string>(onlySkillNames, StringComparer.OrdinalIgnoreCase);

        try
        {
            var count = await WriteSkillSynonymsAsync(
                scope, code, (skill, _) => skillFilter == null || skillFilter.Contains(skill.Name));
            if (count == null)
                return;

            _logger.LogInformation(
                "Installed skill synonyms for language plugin '{Code}': {Count} skill(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install skill synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Startup backfill of the pack's skill synonyms. Writes only the skills that carry no synonyms of
    /// this language yet - a skill seeded after the pack was installed, or one the pack file gained
    /// later - and leaves every other skill without a write: IAgentSkillRepository.UpdateAsync commits
    /// per call, so a full reinstall of every pack on every boot would rewrite hundreds of unchanged rows.
    /// A written skill ends in the same state a fresh install produces (jsonb mirror plus skill_phrase
    /// rows). The presence check ignores case so a key stored as zh-CN is not duplicated as zh-cn.
    /// </summary>
    /// <param name="scope">Scope providing the skill and phrase repositories</param>
    /// <param name="code">Language code of the pack, spelled as in its manifest</param>
    public async Task BackfillMissingSkillSynonymsAsync(IServiceScope scope, string code)
    {
        try
        {
            var count = await WriteSkillSynonymsAsync(
                scope, code, (skill, keywords) => keywords is { Count: > 0 } && !HasSynonymsFor(skill, code));
            if (count == null)
                return;

            _logger.LogInformation(
                "Backfilled skill synonyms for language plugin '{Code}': {Count} skill(s) without that language updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backfill skill synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Shared core of install and backfill: writes the pack synonyms of every enabled skill the pack names
    /// and <paramref name="include"/> accepts. Returns null when the pack has no skill synonym file or an
    /// empty one, so the callers log nothing.
    /// </summary>
    /// <param name="include">Decides per skill, given the pack phrases for it, whether it is written</param>
    private async Task<int?> WriteSkillSynonymsAsync(
        IServiceScope scope, string code, Func<AgentSkill, List<string>, bool> include)
    {
        var synonymsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.SkillSynonymsFileName);
        if (!File.Exists(synonymsPath))
            return null;

        var json = File.ReadAllText(synonymsPath);
        var synonymMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
        if (synonymMap == null || synonymMap.Count == 0)
            return null;

        var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
        var phraseRepo = scope.ServiceProvider.GetRequiredService<ISkillPhraseRepository>();
        var allSkills = await skillRepo.GetAllEnabledTrackedAsync();
        var count = 0;

        foreach (var skill in allSkills)
        {
            if (!synonymMap.TryGetValue(skill.Name, out var keywords) || !include(skill, keywords))
                continue;

            skill.Synonyms ??= new Dictionary<string, List<string>>();
            skill.Synonyms[code] = await LanguagePackPhraseMirror.MergePackIntoMirrorAsync(
                phraseRepo, SkillPhraseOwnerKinds.Skill, skill.Name, code, skill.Synonyms.GetValueOrDefault(code), keywords);
            await skillRepo.UpdateAsync(skill);
            await LanguagePackPhraseMirror.ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Skill, skill.Name, code, keywords);
            count++;
        }

        return count;
    }

    private static bool HasSynonymsFor(AgentSkill skill, string code) =>
        skill.Synonyms != null
        && skill.Synonyms.Any(entry =>
            string.Equals(entry.Key, code, StringComparison.OrdinalIgnoreCase) && entry.Value is { Count: > 0 });

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
            var allSkills = await skillRepo.GetAllEnabledTrackedAsync();
            var count = 0;

            foreach (var skill in allSkills)
            {
                if (!synonymMap.ContainsKey(skill.Name))
                    continue;

                if (skill.Synonyms == null || !skill.Synonyms.ContainsKey(code))
                    continue;

                LanguagePackPhraseMirror.SetOrRemove(skill.Synonyms, code, await LanguagePackPhraseMirror.MergePackIntoMirrorAsync(
                    phraseRepo, SkillPhraseOwnerKinds.Skill, skill.Name, code, skill.Synonyms[code], []));
                await skillRepo.UpdateAsync(skill);
                await LanguagePackPhraseMirror.ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Skill, skill.Name, code, []);
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
                recipe.Synonyms[code] = await LanguagePackPhraseMirror.MergePackIntoMirrorAsync(
                    phraseRepo, SkillPhraseOwnerKinds.Recipe, recipe.Name, code, recipe.Synonyms.GetValueOrDefault(code), keywords);
                await recipeRepo.UpdateAsync(recipe);
                await LanguagePackPhraseMirror.ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Recipe, recipe.Name, code, keywords);
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

                LanguagePackPhraseMirror.SetOrRemove(recipe.Synonyms, code, await LanguagePackPhraseMirror.MergePackIntoMirrorAsync(
                    phraseRepo, SkillPhraseOwnerKinds.Recipe, recipe.Name, code, recipe.Synonyms[code], []));
                await recipeRepo.UpdateAsync(recipe);
                await LanguagePackPhraseMirror.ReplacePackPhrasesAsync(phraseRepo, SkillPhraseOwnerKinds.Recipe, recipe.Name, code, []);
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

    /// <summary>
    /// Reconciles the pack's navigation synonyms row by row for source "plugin" only. The former
    /// replace deleted every row of the (target, language) pair — including customer-trained "user"
    /// synonyms — on each reinstall and uninstall.
    /// </summary>
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
                await synonymRepo.SyncSourceKeywordsForTargetLanguageAsync(targetId, code, entry.Synonyms ?? [], SynonymSources.Plugin);
                count++;
            }

            var stale = await synonymRepo.RemoveSourceRowsForLanguageExceptAsync(code, SynonymSources.Plugin, overlay.Keys.ToList());

            await WarmUpNavigationCacheAsync(scope);

            _logger.LogInformation(
                "Installed navigation synonyms for language plugin '{Code}': {Count} target(s) updated, {Stale} stale row(s) removed",
                code.ForLog(), count, stale);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install navigation synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    // Removes every plugin row of the language, not only those of the targets the current pack file
    // lists: a target an earlier pack version had and this one dropped or renamed would otherwise stay.
    public async Task UninstallNavigationSynonymsAsync(IServiceScope scope, string code)
    {
        try
        {
            var synonymRepo = scope.ServiceProvider.GetRequiredService<INavigationTargetSynonymRepository>();
            var removed = await synonymRepo.RemoveSourceRowsForLanguageExceptAsync(code, SynonymSources.Plugin, []);

            await WarmUpNavigationCacheAsync(scope);

            _logger.LogInformation(
                "Uninstalled navigation synonyms for language plugin '{Code}': {Count} row(s) removed",
                code.ForLog(), removed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall navigation synonyms for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// The navigation cache refreshes lazily, so without an awaited reload the first request after an
    /// install or uninstall still matched against the previous synonym set.
    /// </summary>
    private static async Task WarmUpNavigationCacheAsync(IServiceScope scope)
    {
        var cache = scope.ServiceProvider.GetService<INavigationTargetCacheService>();
        if (cache != null)
        {
            await cache.WarmUpAsync();
        }
    }

    private sealed class PluginNavigationEntry
    {
        public string[] Synonyms { get; set; } = [];
        public string Status { get; set; } = "generated";
    }
}
