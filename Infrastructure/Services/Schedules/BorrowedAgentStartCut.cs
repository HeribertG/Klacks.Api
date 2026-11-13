// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Start-time cut of the borrowed people for the singleton planning runners (Wizard 1/2/3, AutoWizard): resolves the
/// scoped IBorrowedAgentFilter in a short scope and returns the agents the run plans, in the given order. Runs before the
/// runner checks its limits and takes its run lock, so lock, limits and run see the same agents.
/// </summary>
/// <param name="scopeFactory">Creates the scope the filter is resolved from</param>
/// <param name="agentIds">Agents of the run in their planning order</param>
/// <param name="groupId">Group the run plans for; null = none</param>
/// <param name="analyseToken">Source scenario token of the run; null = main plan</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class BorrowedAgentStartCut
{
    public static async Task<IReadOnlyList<Guid>> RemoveBorrowedAgentsAsync(
        this IServiceScopeFactory scopeFactory,
        IReadOnlyList<Guid> agentIds,
        Guid? groupId,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<IBorrowedAgentFilter>()
            .RemoveBorrowedAsync(agentIds, groupId, analyseToken, cancellationToken);
    }
}
