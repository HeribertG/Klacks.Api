// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that lets the signed-in user choose how deep Klacksy explains from now on: beginner or advanced is stored as the
/// user's own explanation-depth override, auto removes the override so the usage heuristic decides again. The
/// persistence lives in IExplainDepthOverrideService, which the onboarding "I already know Klacks" endpoint shares.
/// </summary>
/// <param name="overrideService">Writes or removes the user's override and drops the cached depth</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(ExplainDepthDefaults.SetSkillName)]
public class SetExplainDepthSkill : BaseSkillImplementation
{
    private const string AllowedValuesText = "beginner, advanced or auto";

    private readonly IExplainDepthOverrideService _overrideService;

    public SetExplainDepthSkill(IExplainDepthOverrideService overrideService)
    {
        _overrideService = overrideService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var depth = GetParameter<string>(parameters, ExplainDepthDefaults.SetSkillParameterName)?.Trim().ToLowerInvariant();
        var outcome = await _overrideService.SetAsync(context.UserId, depth, cancellationToken);

        return outcome switch
        {
            ExplainDepthOverrideOutcome.NoUser =>
                SkillResult.Error("The explanation depth can only be set for a signed-in user."),
            ExplainDepthOverrideOutcome.UnknownDepth =>
                SkillResult.Error($"Unknown explanation depth '{depth}'. Use {AllowedValuesText}."),
            ExplainDepthOverrideOutcome.NoAgent =>
                SkillResult.Error("No agent is configured yet."),
            ExplainDepthOverrideOutcome.Cleared => SkillResult.SuccessResult(
                new { Depth = depth },
                "The explanation depth is automatic again: it follows how experienced the user is. " +
                "Confirm this to the user in one short sentence."),
            _ => SkillResult.SuccessResult(
                new { Depth = depth },
                $"The explanation depth is now '{depth}' for this user and applies from the next answer on until the user " +
                "changes it. Confirm this to the user in one short sentence."),
        };
    }
}
