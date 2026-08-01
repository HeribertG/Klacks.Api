// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns down a learned relation between two abilities via DismissSkillRelationCommand, so it stops
/// counting and is not proposed again.
/// </summary>
/// <param name="relationId">UUID of the relation to turn down (required).</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("dismiss_skill_relation")]
public class DismissSkillRelationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DismissSkillRelationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var relationId = GetRequiredGuid(parameters, "relationId");

        var dismissed = await _mediator.Send(new DismissSkillRelationCommand(relationId), cancellationToken);

        if (dismissed == null)
        {
            return SkillResult.Error($"Learned relation {relationId} not found.");
        }

        return SkillResult.SuccessResult(
            new { dismissed.Id, dismissed.SkillAName, dismissed.SkillBName, dismissed.Type, dismissed.Status },
            $"Relation '{dismissed.SkillAName}' {dismissed.Type} '{dismissed.SkillBName}' turned down.");
    }
}
