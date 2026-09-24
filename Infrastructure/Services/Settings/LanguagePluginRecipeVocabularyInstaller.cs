// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs and uninstalls the pack-owned per-recipe gate vocabulary of a language plugin: the question-word
/// vetoes (recipe-vetoes.json, AgentRecipe.Vetoes) and the subject anchors of the semantic recipe fallback
/// (recipe-anchors.json, AgentRecipe.Anchors). Both are keyed by the pack's language code, owned outright
/// by the pack and never mirrored into skill_phrase, so they share one write and one removal path.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginRecipeVocabularyInstaller
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginRecipeVocabularyInstaller(
        string pluginDirectory,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    /// <summary>
    /// Writes the pack's per-recipe question-word veto vocabulary into AgentRecipe.Vetoes[code].
    /// Two deliberate differences from LanguagePluginContentInstaller.InstallRecipeSynonymsAsync:
    /// nothing is mirrored into skill_phrase, because vetoes are exclusion vocabulary rather than
    /// routing phrases - indexing them would let a question word pull the recipe UP in semantic
    /// retrieval, which is the opposite of what a veto exists for; and the pack owns its language key
    /// outright, so a reinstall replaces it instead of merging, there being no admin-edit path that
    /// also writes this column.
    /// The recipes are loaded through the TRACKING query (GetAllAsync), not GetAllEnabledAsync: this
    /// installer runs in the same scope as the recipe-synonym installer, which has already attached every
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

            var count = await ReplacePackOwnedRecipeVocabularyAsync(
                scope, code, vetoMap, recipe => recipe.Vetoes, (recipe, value) => recipe.Vetoes = value);

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
    /// Writes the pack's per-recipe subject vocabulary into AgentRecipe.Anchors[code], the plugin-language
    /// counterpart of the non-verb allOf conditions that gate the semantic recipe fallback. Same contract
    /// as InstallRecipeVetoesAsync and for the same reasons: nothing is mirrored into skill_phrase
    /// (anchors gate a candidate, they are not routing vocabulary), the pack owns its language key so a
    /// reinstall replaces it, the rows are loaded through the tracking query because the synonym and veto
    /// installers have already attached them in this scope, an unchanged recipe is not rewritten, and a
    /// failure - a malformed file included - is logged and swallowed, leaving the column as it was.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack in manifest spelling</param>
    public async Task InstallRecipeAnchorsAsync(IServiceScope scope, string code)
    {
        var anchorsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.RecipeAnchorsFileName);
        if (!File.Exists(anchorsPath))
            return;

        try
        {
            var json = File.ReadAllText(anchorsPath);
            var anchorMap = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json, JsonOptions);
            if (anchorMap == null || anchorMap.Count == 0)
                return;

            var count = await ReplacePackOwnedRecipeVocabularyAsync(
                scope, code, anchorMap, recipe => recipe.Anchors, (recipe, value) => recipe.Anchors = value);

            _logger.LogInformation(
                "Installed recipe anchors for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install recipe anchors for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// The shared write of a pack-owned per-recipe vocabulary column (vetoes, anchors): replaces the
    /// language key on every enabled recipe the pack names, skipping recipes whose stored list is already
    /// identical, and returns how many recipes were written.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack</param>
    /// <param name="vocabularyByRecipe">Pack file content, recipe name to terms</param>
    /// <param name="read">Reads the column from a recipe</param>
    /// <param name="write">Assigns the column on a recipe</param>
    private static async Task<int> ReplacePackOwnedRecipeVocabularyAsync(
        IServiceScope scope,
        string code,
        Dictionary<string, List<string>> vocabularyByRecipe,
        Func<AgentRecipe, Dictionary<string, List<string>>?> read,
        Action<AgentRecipe, Dictionary<string, List<string>>> write)
    {
        var recipeRepo = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
        var allRecipes = await recipeRepo.GetAllAsync();
        var count = 0;

        foreach (var recipe in allRecipes.Where(recipe => recipe.IsEnabled))
        {
            if (!vocabularyByRecipe.TryGetValue(recipe.Name, out var terms) || terms is null)
                continue;

            var cleaned = terms
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var column = read(recipe);
            if (column != null
                && column.TryGetValue(code, out var installed)
                && installed.SequenceEqual(cleaned, StringComparer.Ordinal))
                continue;

            if (column == null)
            {
                column = new Dictionary<string, List<string>>();
                write(recipe, column);
            }

            column[code] = cleaned;
            await recipeRepo.UpdateAsync(recipe);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Removes the pack's language key from every recipe that carries it. Driven by the column, not by
    /// the pack file: at uninstall time the file may already be gone, and after a recipe rename it no
    /// longer names the row that still carries the key. Loaded through the tracking query for the same
    /// reason as InstallRecipeVetoesAsync - the recipe-synonym uninstaller has already attached the rows
    /// in this scope.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack</param>
    public async Task UninstallRecipeVetoesAsync(IServiceScope scope, string code)
    {
        try
        {
            var count = await RemovePackOwnedRecipeVocabularyAsync(scope, code, recipe => recipe.Vetoes);

            _logger.LogInformation(
                "Uninstalled recipe vetoes for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall recipe vetoes for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// Removes the pack's language key from AgentRecipe.Anchors on every recipe that carries it. Driven by
    /// the column, not by the pack file, and loaded through the tracking query, both for the reasons
    /// UninstallRecipeVetoesAsync gives.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack in manifest spelling</param>
    public async Task UninstallRecipeAnchorsAsync(IServiceScope scope, string code)
    {
        try
        {
            var count = await RemovePackOwnedRecipeVocabularyAsync(scope, code, recipe => recipe.Anchors);

            _logger.LogInformation(
                "Uninstalled recipe anchors for language plugin '{Code}': {Count} recipe(s) updated",
                code.ForLog(), count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall recipe anchors for language plugin '{Code}'", code.ForLog());
        }
    }

    /// <summary>
    /// The shared removal of a pack-owned per-recipe vocabulary column: drops the language key from
    /// every recipe that carries it and returns how many recipes were written.
    /// </summary>
    /// <param name="scope">Scope providing the recipe repository</param>
    /// <param name="code">Language code of the pack</param>
    /// <param name="read">Reads the column from a recipe</param>
    private static async Task<int> RemovePackOwnedRecipeVocabularyAsync(
        IServiceScope scope, string code, Func<AgentRecipe, Dictionary<string, List<string>>?> read)
    {
        var recipeRepo = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
        var allRecipes = await recipeRepo.GetAllAsync();
        var count = 0;

        foreach (var recipe in allRecipes)
        {
            var column = read(recipe);
            if (column == null || !column.Remove(code))
                continue;

            await recipeRepo.UpdateAsync(recipe);
            count++;
        }

        return count;
    }
}
