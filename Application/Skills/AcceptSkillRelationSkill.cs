// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Confirms a learned relation between two abilities via AcceptSkillRelationCommand, so it counts
/// from then on instead of waiting for review.
/// </summary>
/// <param name="relationId">UUID of the relation to confirm (required).</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("accept_skill_relation")]
public class AcceptSkillRelationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public AcceptSkillRelationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var relationId = GetRequiredGuid(parameters, "relationId");

        var accepted = await _mediator.Send(new AcceptSkillRelationCommand(relationId), cancellationToken);

        if (accepted == null)
        {
            return SkillResult.Error($"Learned relation {relationId} not found.");
        }

        return SkillResult.SuccessResult(
            new { accepted.Id, accepted.SkillAName, accepted.SkillBName, accepted.Type, accepted.Status },
            $"Relation '{accepted.SkillAName}' {accepted.Type} '{accepted.SkillBName}' confirmed.");
    }
}
