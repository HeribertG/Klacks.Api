// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the configured counter rules via ListQuery so the agent can pick a counterRuleId before
/// update_counter_rule / delete_counter_rule.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_counter_rules")]
public class ListCounterRulesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListCounterRulesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rules = await _mediator.Send(new ListQuery<CounterRuleResource>(), cancellationToken);

        var projected = rules
            .Select(r => new
            {
                r.Id,
                EventType = r.EventType.ToString(),
                Period = r.Period.ToString(),
                r.Threshold,
                r.HoursThreshold,
                Enforcement = r.Enforcement?.ToString(),
                r.SchedulingRuleId
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, CounterRules = projected },
            $"Found {projected.Count} counter rule(s).");
    }
}
