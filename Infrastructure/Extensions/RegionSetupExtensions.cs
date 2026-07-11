// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Startup extension that applies the optional one-time region setup profile.
/// </summary>
/// <param name="app">The web application whose service provider supplies the scoped region setup service</param>

using Klacks.Api.Application.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Extensions;

public static class RegionSetupExtensions
{
    public static async Task ApplyRegionSetupAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IRegionSetupService>();
        await service.ApplyAsync();
    }
}
