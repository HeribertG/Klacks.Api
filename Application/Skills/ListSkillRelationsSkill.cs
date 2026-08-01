// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the learned relations between two abilities via GetSkillRelationsQuery so the agent can
/// pick a relationId before accept_skill_relation / dismiss_skill_relation.
/// </summary>
/// <param name="status">Optional filter on the review state, for example Proposed.</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_skill_relations")]
public class ListSkillRelationsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListSkillRelationsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var relations = await _mediator.Send(new GetSkillRelationsQuery(), cancellationToken);

        var status = GetParameter<string>(parameters, "status")?.Trim();
        if (!string.IsNullOrWhiteSpace(status))
        {
            relations = relations
                .Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var projected = relations
            .OrderByDescending(r => r.Confidence)
            .Select(r => new
            {
                r.Id,
                r.SkillAName,
                r.SkillBName,
                r.Type,
                r.Confidence,
                r.SupportCount,
                r.ContradictionCount,
                r.Source,
                r.Status,
                r.LastReinforcedAt
            })
            .ToList();

        var scope = string.IsNullOrWhiteSpace(status) ? string.Empty : $" with status {status}";

        return SkillResult.SuccessResult(
            new { Count = projected.Count, SkillRelations = projected },
            $"Found {projected.Count} learned relation(s){scope}.");
    }
}
