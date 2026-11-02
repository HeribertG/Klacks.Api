// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the scenario behind an analyse token and applies the scenario visibility rule of ScenarioGroupVisibility to it.
/// The token of a variant (key) is refused for every caller, unrestricted ones (admins, background jobs without a user)
/// included, because a variant carries shifts only; IsVariantTokenVisibleAsync is the one door for actions marked with
/// AllowVariantToken. For every other token unrestricted callers are allowed as before, including unknown or
/// just-deleted tokens - the same rule ClientVisibilityGuard follows - but the token is now loaded once to tell a variant
/// from a scenario.
/// </summary>
/// <param name="scenarioRepository">Resolves the (not deleted) scenario by its token</param>
/// <param name="groupVisibilityGuard">Decides admin status and the caller's visible groups; no user means unrestricted</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;

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
        if (analyseToken is not { } token)
        {
            return true;
        }

        var scenario = await _scenarioRepository.GetByTokenAsync(token, cancellationToken);
        if (scenario?.Kind == AnalyseScenarioKind.Variant)
        {
            return false;
        }

        return await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken)
            || await _groupVisibilityGuard.IsScenarioVisibleAsync(scenario, cancellationToken);
    }

    public async Task<bool> IsVariantTokenVisibleAsync(Guid analyseToken, CancellationToken cancellationToken = default)
    {
        var scenario = await _scenarioRepository.GetByTokenAsync(analyseToken, cancellationToken);
        return scenario?.Kind == AnalyseScenarioKind.Variant
            && await _groupVisibilityGuard.IsScenarioVisibleAsync(scenario, cancellationToken);
    }

    public async Task<bool> IsHiddenVariantTokenAsync(Guid analyseToken, CancellationToken cancellationToken = default)
    {
        var scenario = await _scenarioRepository.GetByTokenAsync(analyseToken, cancellationToken);
        return scenario?.Kind == AnalyseScenarioKind.Variant
            && !await _groupVisibilityGuard.IsScenarioVisibleAsync(scenario, cancellationToken);
    }

    public async Task EnsureVisibleAsync(Guid? analyseToken, CancellationToken cancellationToken = default)
    {
        if (!await IsVisibleAsync(analyseToken, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ScenarioTokenNotFoundFormat, analyseToken));
        }
    }
}
