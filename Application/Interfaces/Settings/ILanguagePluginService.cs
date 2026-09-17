// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Config;

namespace Klacks.Api.Application.Interfaces.Settings;

public interface ILanguagePluginService
{
    Task InitializeAsync();
    IReadOnlyList<LanguagePluginInfo> GetAllPlugins();
    LanguagePluginInfo? GetPlugin(string code);
    Task<bool> InstallAsync(string code);
    Task<bool> UninstallAsync(string code);
    Dictionary<string, string>? GetTranslations(string code);
    IReadOnlyList<string> GetInstalledPluginCodes();
    Task<string?> GetPluginDocAsync(string code, string manualName);
    Task RefreshPluginsAsync();

    /// <summary>
    /// Writes the skill synonyms of every installed language pack into the named skills. A pack only
    /// reaches the skills that are enabled when it is installed, so a feature plugin enabled later has
    /// to pull the synonyms of its skills itself.
    /// </summary>
    /// <param name="skillNames">Names of the skills to update; every other skill stays untouched</param>
    Task ApplyInstalledSkillSynonymsAsync(IReadOnlyCollection<string> skillNames);

    /// <summary>
    /// Writes the recipe veto vocabulary of every installed language pack into the enabled recipes.
    /// A startup backfill rather than a feature-plugin hook like the skill variant above: recipe rows
    /// come from the recipe seeder, and a pack only ever reaches the recipes enabled at install time,
    /// so without this a veto column added after the packs were installed stays empty on every existing
    /// installation. Callers must invoke it AFTER both the language-plugin initialization and the
    /// recipe seeding have completed - in Program.cs those two are parallel branches of one batch.
    /// </summary>
    Task ApplyInstalledRecipeVetoesAsync();

    /// <summary>
    /// Writes the user-facing skill labels of every installed language pack into the enabled skills.
    /// A startup backfill for the same reason as ApplyInstalledRecipeVetoesAsync: a pack only reaches
    /// the skills that exist at the moment it is installed, so a label column added after the packs were
    /// installed stays empty on every existing installation. Deliberately NOT called from
    /// InitializeAsync - it depends on the skill rows the chained
    /// InitializeFeaturePluginsThenLoadSkillSeedsAsync branch creates, which is a parallel branch of the
    /// same Task.WhenAll in Program.cs, so callers must invoke it only after that batch completed.
    /// </summary>
    Task ApplyInstalledSkillLabelsAsync();
}
