// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Infrastructure.Persistence.Seed;

namespace Klacks.Api.Infrastructure.Extensions;

public static class AssistantExtensions
{
    public static async Task InitializeSkillRegistryAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var initializer = scope.ServiceProvider.GetRequiredService<SkillRegistryInitializer>();
        await initializer.InitializeAsync();
    }

    public static async Task<IApplicationBuilder> SeedGlobalAgentRulesAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<GlobalAgentRuleSeedService>();
        await seedService.SeedAsync();
        return app;
    }

    public static async Task<IApplicationBuilder> SeedAgentSoulSectionsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<AgentSoulSectionSeedService>();
        await seedService.SeedAsync();
        return app;
    }

    public static async Task<IApplicationBuilder> SeedUiControlsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<UiControlSeedService>();
        await seedService.SeedAsync();
        return app;
    }

    public static async Task<IApplicationBuilder> SeedEmailFoldersAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<EmailFolderSeedService>();
        await seedService.SeedAsync();
        return app;
    }

    public static async Task<IApplicationBuilder> LoadSkillSeedsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<SkillSeedLoader>();
        await loader.LoadAsync();
        return app;
    }
}
