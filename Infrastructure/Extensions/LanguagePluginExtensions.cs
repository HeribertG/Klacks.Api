// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Services.Settings;

namespace Klacks.Api.Infrastructure.Extensions;

public static class LanguagePluginExtensions
{
    public static IApplicationBuilder InitializeLanguagePlugins(this IApplicationBuilder app)
    {
        var service = app.ApplicationServices.GetRequiredService<ILanguagePluginService>();

        if (service is LanguagePluginService pluginService)
        {
            pluginService.Initialize();
        }

        return app;
    }
}
