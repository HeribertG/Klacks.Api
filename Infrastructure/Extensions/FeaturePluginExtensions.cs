// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Extension method for initializing feature plugins during application startup.
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
}
