// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs and uninstalls content-related data (docs, skill synonyms, recipe synonyms, navigation synonyms, sentiment keywords, translations)
/// for language plugins.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Klacksy;
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

    /// <summary>
    /// Writes the pack's per-recipe question-word veto vocabulary into AgentRecipe.Vetoes[code].
    /// Two deliberate differences from InstallRecipeSynonymsAsync:
    /// nothing is mirrored into skill_phrase, because vetoes are exclusion vocabulary rather than
    /// routing phrases - indexing them would let a question word pull the recipe UP in semantic
    /// retrieval, which is the opposite of what a veto exists for; and the pack owns its language key
    /// outright, so a reinstall replaces it instead of merging, there being no admin-edit path that
    /// also writes this column.
    /// The recipes are loaded through the TRACKING query (GetAllAsync), not GetAllEnabledAsync: this
    /// installer runs in the same scope as InstallRecipeSynonymsAsync, which has already attached every
    /// recipe row, and the startup backfill runs all installed languages through one scope. A second
    /// no-tracking read would hand Update a second instance of an already-tracked key, which throws and
    /// is swallowed below - the pack would install its synonyms but silently no vetoes. A recipe whose
    /// vocabulary is already current is skipped, so a startup backfill is a no-op write-wise.
    /// Like every other install here a failure is logged and swallowed, so a malformed pack file is a
    /// silent no-op while the unit gates parse the same file successfully. "Gate green / runtime empty"
    /// is therefore possible; the error log is the only place it shows.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack</param>
    public async Task InstallRecipeVetoesAsync(IServiceScope scope, string code)
    {
        var vetoesPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.RecipeVetoesFileName);
        if (!File.Exists(vetoesPath))
            return;

        try
        {
            var json = File.ReadAllText(vetoesPath);
            var vetoMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (vetoMap == null || vetoMap.Count == 0)
                return;

            var recipeRepo = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
            var allRecipes = await recipeRepo.GetAllAsync();
            var count = 0;

            foreach (var recipe in allRecipes.Where(recipe => recipe.IsEnabled))
            {
                if (!vetoMap.TryGetValue(recipe.Name, out var terms))
                    continue;

                var cleaned = terms
                    .Where(term => !string.IsNullOrWhiteSpace(term))
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                if (recipe.Vetoes != null
                    && recipe.Vetoes.TryGetValue(code, out var installed)
                    && installed.SequenceEqual(cleaned, StringComparer.Ordinal))
                    continue;

                recipe.Vetoes ??= new Dictionary<string, List<string>>();
                recipe.Vetoes[code] = cleaned;
                await recipeRepo.UpdateAsync(recipe);
                count++;
            }

            _logger.LogInformation(
                "Installed recipe vetoes for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install recipe vetoes for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the pack's language key from every recipe that carries it. Driven by the column, not by
    /// the pack file: at uninstall time the file may already be gone, and after a recipe rename it no
    /// longer names the row that still carries the key. Loaded through the tracking query for the same
    /// reason as InstallRecipeVetoesAsync - UninstallRecipeSynonymsAsync has already attached the rows
    /// in this scope.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack</param>
    public async Task UninstallRecipeVetoesAsync(IServiceScope scope, string code)
    {
        try
        {
            var recipeRepo = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
            var allRecipes = await recipeRepo.GetAllAsync();
            var count = 0;

            foreach (var recipe in allRecipes)
            {
                if (recipe.Vetoes == null || !recipe.Vetoes.Remove(code))
                    continue;

                await recipeRepo.UpdateAsync(recipe);
                count++;
            }

            _logger.LogInformation(
                "Uninstalled recipe vetoes for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall recipe vetoes for language plugin '{Code}'", code.ForLog());
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
    /// Installs the plugin's subdivisions from {code}/states.json: inserts each entry as a new
    /// selectable state if it doesn't exist yet, or merges its name translations into the existing
    /// row otherwise. A plugin country ships its own subdivisions (e.g. the Spanish autonomous
    /// communities that come with the "es" plugin) that are not part of the core seed, so those
    /// rows need upsert semantics instead of the update-only merge used for calendar rules.
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

                var existing = await db.State.FirstOrDefaultAsync(s => s.Id == id);

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
                        Id = id,
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
