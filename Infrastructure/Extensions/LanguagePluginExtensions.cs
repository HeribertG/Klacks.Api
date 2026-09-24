// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Extensions;

public static class LanguagePluginExtensions
{
    public static async Task<IApplicationBuilder> InitializeLanguagePluginsAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<ILanguagePluginService>();
        await service.InitializeAsync();

        return app;
    }

    /// <summary>
    /// Backfills the recipe veto vocabulary of every installed language pack. Must run after the
    /// parallel startup seeding batch rather than inside it: it needs both the installed-codes list
    /// InitializeLanguagePluginsAsync loads and the recipe rows LoadRecipeSeedsAsync creates, and those
    /// two are parallel branches of one Task.WhenAll.
    /// </summary>
    public static async Task<IApplicationBuilder> BackfillRecipeVetoesAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<ILanguagePluginService>();
        await service.ApplyInstalledRecipeVetoesAsync();

        return app;
    }

    /// <summary>
    /// Backfills the recipe anchor vocabulary of every installed language pack. Same ordering constraint
    /// as BackfillRecipeVetoesAsync: it needs the installed-codes list and the seeded recipe rows, which
    /// are parallel branches of one Task.WhenAll.
    /// </summary>
    public static async Task<IApplicationBuilder> BackfillRecipeAnchorsAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<ILanguagePluginService>();
        await service.ApplyInstalledRecipeAnchorsAsync();

        return app;
    }

    /// <summary>
    /// Backfills the user-facing skill labels of every installed language pack. Same ordering constraint
    /// as BackfillRecipeVetoesAsync, one branch further along: it needs the installed-codes list from
    /// InitializeLanguagePluginsAsync AND the skill rows from the chained
    /// InitializeFeaturePluginsThenLoadSkillSeedsAsync branch, and both are parallel branches of one
    /// Task.WhenAll.
    /// </summary>
    public static async Task<IApplicationBuilder> BackfillSkillLabelsAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<ILanguagePluginService>();
        await service.ApplyInstalledSkillLabelsAsync();

        return app;
    }

    /// <summary>
    /// Backfills the skill synonyms of every installed language pack into the skills that have none of
    /// that language yet. Same ordering constraint as BackfillSkillLabelsAsync (installed codes AND the
    /// seeded skill rows), and it must run before InitializeSkillRegistryAsync and before app.Run():
    /// the registry reads the synonyms, and KnowledgeIndexStartupService embeds the new phrases at host
    /// start - a later placement would leave both on the old vocabulary until the next restart.
    /// </summary>
    public static async Task<IApplicationBuilder> BackfillSkillSynonymsAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<ILanguagePluginService>();
        await service.ApplyInstalledSkillSynonymBackfillAsync();

        return app;
    }
}
