// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Start-time scenario check for the singleton planning runners (Wizard 1/2/3, AutoWizard, benchmark). It resolves the
/// scoped IScenarioTokenAccessGuard in a short scope while the starting request is still running, because the job itself
/// runs later without HttpContext and would then count as an unrestricted background caller. Must run before the
/// runner takes its run lock, so a refused start leaves no lock behind.
/// </summary>
/// <param name="scopeFactory">Creates the scope the guard is resolved from</param>
/// <param name="analyseToken">Source scenario token of the run; null = main plan</param>

using Klacks.Api.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class ScenarioTokenStartCheck
{
    public static async Task EnsureScenarioTokenVisibleAsync(
        this IServiceScopeFactory scopeFactory, Guid? analyseToken, CancellationToken cancellationToken)
    {
        if (analyseToken is null)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var guard = scope.ServiceProvider.GetRequiredService<IScenarioTokenAccessGuard>();
        await guard.EnsureVisibleAsync(analyseToken, cancellationToken);
    }
}
