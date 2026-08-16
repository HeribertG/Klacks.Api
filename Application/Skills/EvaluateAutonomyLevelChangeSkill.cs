// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates a proposed autonomy level change so Klacksy can explain its concrete effect before the
/// user confirms it: which skill risk classes would start or stop running without a confirmation
/// prompt, and how many currently registered skills fall into each. Thin wrapper that dispatches
/// <see cref="Klacks.Api.Application.Queries.Assistant.EvaluateAutonomyLevelChangeQuery"/>. The
/// evaluation is advisory only — changing the level stays a manual, always-confirmed step
/// (set_autonomy_level).
/// </summary>
/// <param name="level">The autonomy level to evaluate: 0-3 or one of Propose, Assisted, Autonomous, FullyAutonomous</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("evaluate_autonomy_level_change")]
public class EvaluateAutonomyLevelChangeSkill : BaseSkillImplementation
{
    private const string LevelParameter = "level";

    private readonly IMediator _mediator;

    public EvaluateAutonomyLevelChangeSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rawLevel = GetParameter<object>(parameters, LevelParameter);
        if (rawLevel == null)
        {
            return SkillResult.Error($"Missing required parameter '{LevelParameter}' (0-3).");
        }

        if (!AutonomyLevelParameterParser.TryParse(rawLevel.ToString(), out var targetLevel))
        {
            return SkillResult.Error(
                $"Invalid autonomy level '{rawLevel}'. Use {(int)AutonomyDefaults.MinimumLevel}-{(int)AutonomyDefaults.MaximumLevel} " +
                $"or one of: {string.Join(", ", Enum.GetNames<AutonomyLevel>())}.");
        }

        var result = await _mediator.Send(
            new EvaluateAutonomyLevelChangeQuery(context.UserId, targetLevel), cancellationToken);

        return SkillResult.SuccessResult(result, result.Recommendation);
    }
}
