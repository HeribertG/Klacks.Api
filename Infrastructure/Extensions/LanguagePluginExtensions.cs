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
}
