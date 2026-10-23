// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Startup extension that seeds the installer-provided initial auto-update policy.
/// </summary>
/// <param name="app">The web application whose service provider supplies the scoped seed service</param>

using Klacks.Api.Infrastructure.Persistence.Seed;

namespace Klacks.Api.Infrastructure.Extensions;

public static class UpdatePolicySeedExtensions
{
    public static async Task SeedUpdatePolicyAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<UpdatePolicySeedService>();
        await seedService.SeedAsync();
    }
}
