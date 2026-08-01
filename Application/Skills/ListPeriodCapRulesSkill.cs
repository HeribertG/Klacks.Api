// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the configured period cap rules via ListQuery so the agent can pick a periodCapRuleId
/// before update_period_cap_rule / delete_period_cap_rule.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_period_cap_rules")]
public class ListPeriodCapRulesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListPeriodCapRulesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rules = await _mediator.Send(new ListQuery<PeriodCapRuleResource>(), cancellationToken);

        var projected = rules
            .Select(r => new
            {
                r.Id,
                Period = r.Period.ToString(),
                Scope = r.Scope.ToString(),
                r.CapHours,
                r.WarnAtPercent,
                r.CustomPeriodWeeks,
                r.RollingWindowWeeks,
                r.MaxAverageWeeklyHours,
                r.SchedulingRuleId
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, PeriodCapRules = projected },
            $"Found {projected.Count} period cap rule(s).");
    }
}
