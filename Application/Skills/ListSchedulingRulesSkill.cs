// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the configured scheduling rules (the named rule sets that carry per-contract caps such as
/// max work days, min rest days, min pause hours, max daily/weekly hours and max consecutive days).
/// Klacksy uses this to know which rule sets exist before reasoning about or explaining the limits a
/// plan must respect. Read-only; takes no parameters.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_scheduling_rules")]
public class ListSchedulingRulesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListSchedulingRulesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rules = await _mediator.Send(new ListQuery<SchedulingRuleResource>(), cancellationToken);

        var list = (rules ?? []).Select(r => new
        {
            r.Id,
            r.Name,
            r.MaxWorkDays,
            r.MinRestDays,
            r.MinPauseHours,
            r.MaxDailyHours,
            r.MaxWeeklyHours,
            r.MaxConsecutiveDays,
            r.DefaultWorkingHours,
            r.OvertimeThreshold,
            r.GuaranteedHours,
            r.MaximumHours,
            r.MinimumHours,
            r.VacationDaysPerYear
        }).ToList();

        var data = new
        {
            Count = list.Count,
            Rules = list
        };

        var message = list.Count == 0
            ? "No scheduling rules configured."
            : $"{list.Count} scheduling rule(s) configured.";

        return SkillResult.SuccessResult(data, message);
    }
}
