// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the scenario behind an analyse token and applies the scenario visibility rule of ScenarioGroupVisibility to it.
/// Unrestricted callers (admins, background jobs without a user) are allowed without a query, so their behaviour for
/// unknown or just-deleted tokens stays as before - the same rule ClientVisibilityGuard follows.
/// </summary>
/// <param name="scenarioRepository">Resolves the (not deleted) scenario by its token</param>
/// <param name="groupVisibilityGuard">Decides admin status and the caller's visible groups; no user means unrestricted</param>

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Services.Groups;

public class ScenarioTokenAccessGuard : IScenarioTokenAccessGuard
{
    public const string ScenarioTokenNotFoundFormat = "Scenario with token {0} not found";

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public ScenarioTokenAccessGuard(IAnalyseScenarioRepository scenarioRepository, IGroupVisibilityGuard groupVisibilityGuard)
    {
        _scenarioRepository = scenarioRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<bool> IsVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        if (analyseToken is not { } token || await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            return true;
        }

        var scenario = await _scenarioRepository.GetByTokenAsync(token, cancellationToken);
        return await _groupVisibilityGuard.IsScenarioVisibleAsync(scenario, cancellationToken);
    }

    public async Task EnsureVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        if (!await IsVisibleAsync(analyseToken, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ScenarioTokenNotFoundFormat, analyseToken));
        }
    }
}
