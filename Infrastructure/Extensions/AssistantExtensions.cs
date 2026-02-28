// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Infrastructure.Persistence.Seed;

namespace Klacks.Api.Infrastructure.Extensions;

public static class AssistantExtensions
{
    public static IApplicationBuilder InitializeSkills(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var registrationService = scope.ServiceProvider.GetRequiredService<SkillRegistrationService>();
        registrationService.RegisterAllSkills();
        return app;
    }

    public static async Task<IApplicationBuilder> SeedAgentSkillsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<AgentSkillSeedService>();
        await seedService.SeedAsync();
        return app;
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
}
