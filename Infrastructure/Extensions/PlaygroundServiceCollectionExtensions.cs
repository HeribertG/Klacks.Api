// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the Playground daily token limit and hardening: the options, the mode switch, the in-memory budget,
/// the device token service, the status service, the model pin and the block and prompt-limit filters. Everything is registered on every instance; each part is inert
/// unless Deployment:IsPlayground is true.
/// </summary>
/// <param name="configuration">Application configuration holding Playground:TokenLimit, Playground:Models and Deployment:IsPlayground</param>

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Interfaces.Playground;
using Klacks.Api.Application.Klacksy;
using Klacks.Api.Application.Services.Playground;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Services.Playground;
using Klacks.Api.Presentation.Filters;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Klacks.Api.Infrastructure.Extensions;

public static class PlaygroundServiceCollectionExtensions
{
    public static IServiceCollection AddPlaygroundTokenLimit(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PlaygroundTokenLimitOptions>()
            .Configure(options => options.ApplyConfiguration(configuration.GetSection(PlaygroundTokenLimitOptions.SectionName)));

        services.AddOptions<PlaygroundModelOptions>()
            .Configure(options => options.ApplyConfiguration(configuration.GetSection(PlaygroundHardeningConstants.ModelsSectionName)));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPlaygroundMode, PlaygroundMode>();
        services.AddSingleton<IPlaygroundTokenBudget, PlaygroundTokenBudget>();
        services.AddSingleton<IPlaygroundDeviceTokenService, PlaygroundDeviceTokenService>();
        services.AddSingleton<IPlaygroundStatusService, PlaygroundStatusService>();
        services.AddSingleton<IPlaygroundModelPin, PlaygroundModelPin>();
        services.AddSingleton<IPlaygroundConversationStore, PlaygroundConversationStore>();
        services.AddTransient<PlaygroundTokenLimitFilter>();
        services.AddTransient<BlockedOnPlaygroundFilter>();
        services.AddTransient<BlockedSettingKeysOnPlaygroundFilter>();
        services.AddTransient<BlockedSetupOnboardingOnPlaygroundFilter>();
        services.AddTransient<PlaygroundPromptLimitFilter>();

        return services;
    }

    /// <summary>
    /// Registers the navigation feedback logger behind its Playground decorator, which swallows the log on the Playground
    /// and forwards every call unchanged everywhere else.
    /// </summary>
    public static IServiceCollection AddPlaygroundNavigationFeedbackLogger(this IServiceCollection services)
    {
        services.AddScoped<NavigationFeedbackLogger>();
        services.AddScoped<INavigationFeedbackLogger>(provider => new PlaygroundSilentNavigationFeedbackLogger(
            provider.GetRequiredService<NavigationFeedbackLogger>(),
            provider.GetRequiredService<IPlaygroundMode>()));

        return services;
    }
}
