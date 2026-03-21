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
}
