// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Central scenario-scope check for every skill that takes the "analyseToken" parameter (read_schedule_state,
/// place_work, start_wizard*, ...). Runs once in the skill executor, before the skill, so no single skill can forget
/// it: a token of a scenario outside the caller's group visibility is refused exactly like an unknown token.
/// </summary>
/// <param name="scenarioTokenAccessGuard">Loads the scenario behind the token and applies the visibility rule</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

public class SkillScenarioScopeGuard : ISkillScenarioScopeGuard
{
    public const string ScenarioNotFoundFormat = "No scenario found for {0} '{1}'.";

    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;

    public SkillScenarioScopeGuard(IScenarioTokenAccessGuard scenarioTokenAccessGuard)
    {
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
    }

    public async Task<SkillResult?> CheckAsync(
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out _) || analyseToken is null)
        {
            return null;
        }

        if (await _scenarioTokenAccessGuard.IsVisibleAsync(analyseToken, cancellationToken))
        {
            return null;
        }

        return SkillResult.Error(string.Format(ScenarioNotFoundFormat, ScenarioScopeParameter.Name, analyseToken));
    }
}
