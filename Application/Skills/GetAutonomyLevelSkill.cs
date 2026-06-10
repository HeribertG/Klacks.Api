// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the user's current autonomy level (0 propose-only, 1 assisted, 2 autonomous,
/// 3 fully autonomous) so the assistant can explain what it may execute without confirmation.
/// </summary>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_autonomy_level")]
public class GetAutonomyLevelSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetAutonomyLevelSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var level = await _mediator.Send(new GetAutonomyLevelQuery(context.UserId), cancellationToken);

        return SkillResult.SuccessResult(
            new { Level = (int)level, Name = level.ToString() },
            $"Current autonomy level is {(int)level} ({level}).");
    }
}
