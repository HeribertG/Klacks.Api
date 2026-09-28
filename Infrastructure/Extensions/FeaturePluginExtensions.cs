// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Extension methods for initializing feature plugins during application startup and for healing the
/// navigation routes they registered.
/// </summary>

using Klacks.Api.Application.Interfaces.Plugins;

namespace Klacks.Api.Infrastructure.Extensions;

public static class FeaturePluginExtensions
{
    public static async Task<IApplicationBuilder> InitializeFeaturePluginsAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<IFeaturePluginService>();
        await service.InitializeAsync();

        return app;
    }

    public static async Task<IApplicationBuilder> SyncFeaturePluginNavigationAsync(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<IFeaturePluginService>();
        await service.SyncNavigationRoutesAsync();

        return app;
    }
}
