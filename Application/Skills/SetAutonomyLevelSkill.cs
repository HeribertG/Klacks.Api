// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets the user's autonomy level (0 propose-only, 1 assisted, 2 autonomous, 3 fully autonomous).
/// Classified as sensitive, so the autonomy gate always demands an explicit user confirmation
/// before the level changes — the assistant can never silently escalate itself.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_autonomy_level")]
public class SetAutonomyLevelSkill : BaseSkillImplementation
{
    private const string LevelParameter = "level";

    private readonly IMediator _mediator;

    public SetAutonomyLevelSkill(IMediator mediator)
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

        if (!TryParseLevel(rawLevel.ToString(), out var level))
        {
            return SkillResult.Error(
                $"Invalid autonomy level '{rawLevel}'. Use {(int)AutonomyDefaults.MinimumLevel}-{(int)AutonomyDefaults.MaximumLevel} " +
                $"or one of: {string.Join(", ", Enum.GetNames<AutonomyLevel>())}.");
        }

        var saved = await _mediator.Send(new SetAutonomyLevelCommand(context.UserId, level), cancellationToken);

        return SkillResult.SuccessResult(
            new { Level = (int)saved, Name = saved.ToString() },
            $"Autonomy level is now {(int)saved} ({saved}).");
    }

    private static bool TryParseLevel(string? value, out AutonomyLevel level)
    {
        level = AutonomyDefaults.DefaultLevel;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (int.TryParse(value, out var numeric))
        {
            if (numeric < (int)AutonomyDefaults.MinimumLevel || numeric > (int)AutonomyDefaults.MaximumLevel)
            {
                return false;
            }

            level = (AutonomyLevel)numeric;
            return true;
        }

        return Enum.TryParse(value, ignoreCase: true, out level)
            && Enum.IsDefined(level);
    }
}
