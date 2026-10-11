// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Combines the client and the scenario visibility rule for by-id schedule row access: a row is visible only when its
/// owning client and its scenario are. Chosen over a mediator behaviour or repository decorators because the by-id
/// handlers already load the row (or its parent Work) and ask the client guard once; swapping that single call keeps one
/// call per handler, needs no second row load and no per-entity decorator, and keeps each handler's own "missing" answer.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the caller may see the owning client</param>
/// <param name="scenarioTokenAccessGuard">Decides whether the caller may see the row's scenario; null token = main plan</param>

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Services.Groups;

public class ScheduleRowAccessGuard : IScheduleRowAccessGuard
{
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;

    public ScheduleRowAccessGuard(
        IClientVisibilityGuard clientVisibilityGuard, IScenarioTokenAccessGuard scenarioTokenAccessGuard)
    {
        _clientVisibilityGuard = clientVisibilityGuard;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
    }

    public async Task<bool> IsVisibleAsync(Guid clientId, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        return await _scenarioTokenAccessGuard.IsVisibleAsync(analyseToken, cancellationToken)
            && await _clientVisibilityGuard.IsVisibleAsync(clientId, cancellationToken);
    }

    public async Task<bool> AreAllVisibleAsync(
        IReadOnlyCollection<Guid> clientIds, Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        return await _scenarioTokenAccessGuard.IsVisibleAsync(analyseToken, cancellationToken)
            && await _clientVisibilityGuard.AreAllVisibleAsync(clientIds, cancellationToken);
    }
}
