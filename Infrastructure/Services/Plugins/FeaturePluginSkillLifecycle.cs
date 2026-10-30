// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What installing, enabling, disabling and uninstalling a feature plugin does to the assistant: seeds and enables the plugin's
/// skills, inserts the curated skill edges that name them, pulls the language pack synonyms and rebuilds the catalogue, or takes the
/// skills out again. A failure here never fails the lifecycle operation: the plugin itself is working at this point and a restart
/// re-runs the seeds, so losing the assistant integration is the smaller failure.
/// </summary>
/// <param name="logger">Logger of the plugin service this work belongs to</param>

using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Persistence.Seed;

namespace Klacks.Api.Infrastructure.Services.Plugins;

internal sealed class FeaturePluginSkillLifecycle
{
    private readonly ILogger _logger;

    public FeaturePluginSkillLifecycle(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Seeds a plugin's skills and puts them into the catalogue and the knowledge index right away.
    /// Enabling is a separate step from seeding on purpose: the seed loader only writes a definition
    /// whose version is strictly greater than the stored one, so a plugin that is installed, removed
    /// and installed again would otherwise come back with its skills still disabled.
    /// </summary>
    /// <param name="scope">The scope whose database work has already been committed</param>
    /// <param name="name">Plugin name, used as the directory name under the plugin root</param>
    /// <param name="reason">What triggered the refresh, for the log line</param>
    public async Task SeedAsync(IServiceScope scope, string name, string reason)
    {
        try
        {
            var seedLoader = scope.ServiceProvider.GetRequiredService<SkillSeedLoader>();
            await seedLoader.SeedPluginSkillsAsync(name);
            await seedLoader.SetPluginSkillsEnabledAsync(name, isEnabled: true);
            await SeedSkillRelationsAsync(scope, name);
            await ApplyLanguagePackSynonymsAsync(scope, seedLoader, name);
            await RefreshCatalogAsync(scope, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed skills for feature plugin '{Name}'", name.ForLog());
        }
    }

    /// <summary>
    /// Takes a plugin's skills out of the assistant without deleting them, and rebuilds the catalogue
    /// so the change is visible before the skill cache expires.
    /// </summary>
    /// <param name="scope">The scope whose database work has already been committed</param>
    /// <param name="name">Plugin name, used as the directory name under the plugin root</param>
    /// <param name="reason">What triggered the refresh, for the log line</param>
    public async Task DisableAsync(IServiceScope scope, string name, string reason)
    {
        try
        {
            var seedLoader = scope.ServiceProvider.GetRequiredService<SkillSeedLoader>();
            await seedLoader.SetPluginSkillsEnabledAsync(name, isEnabled: false);
            await RefreshCatalogAsync(scope, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable skills for feature plugin '{Name}'", name.ForLog());
        }
    }

    /// <summary>
    /// Inserts the curated skill edges that name the plugin's skills. Their seed file belongs to the core and its loader skips an
    /// edge whose skill is missing, so without this step such an edge reached the database only with the next application start.
    /// Insert-only and idempotent; runs before the catalogue refresh and never blocks it.
    /// </summary>
    private async Task SeedSkillRelationsAsync(IServiceScope scope, string name)
    {
        try
        {
            await scope.ServiceProvider.GetRequiredService<SkillRelationSeedLoader>().LoadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to seed the skill relations of feature plugin '{Name}'", name.ForLog());
        }
    }

    /// <summary>
    /// Gives the plugin's skills the synonyms of every installed language pack. A pack installed while
    /// the plugin was off skipped these skills (it only reaches enabled skills), so without this step
    /// the plugin-language synonyms only arrived after the pack was installed again. Runs before the
    /// catalogue refresh so the knowledge index embeds the new phrases, and never blocks that refresh.
    /// </summary>
    private async Task ApplyLanguagePackSynonymsAsync(IServiceScope scope, SkillSeedLoader seedLoader, string name)
    {
        try
        {
            var skillNames = await seedLoader.GetPluginSkillNamesAsync(name);
            if (skillNames.Count == 0)
                return;

            await scope.ServiceProvider.GetRequiredService<ILanguagePluginService>()
                .ApplyInstalledSkillSynonymsAsync(skillNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply language pack synonyms to feature plugin '{Name}'", name.ForLog());
        }
    }

    private static async Task RefreshCatalogAsync(IServiceScope scope, string reason) =>
        await scope.ServiceProvider.GetRequiredService<ISkillCatalogRefresher>().RefreshAsync(reason);
}
