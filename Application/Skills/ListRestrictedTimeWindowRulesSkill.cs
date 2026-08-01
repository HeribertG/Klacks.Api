// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the configured restricted time window rules via ListQuery so the agent can pick a
/// restrictedTimeWindowRuleId before update_restricted_time_window_rule /
/// delete_restricted_time_window_rule.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_restricted_time_window_rules")]
public class ListRestrictedTimeWindowRulesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListRestrictedTimeWindowRulesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rules = await _mediator.Send(new ListQuery<RestrictedTimeWindowRuleResource>(), cancellationToken);

        var projected = rules
            .Select(r => new
            {
                r.Id,
                Season = $"{r.SeasonFromMonth:00}-{r.SeasonFromDay:00} to {r.SeasonToMonth:00}-{r.SeasonToDay:00}",
                DailyStart = r.DailyStart.ToString("HH:mm"),
                DailyEnd = r.DailyEnd.ToString("HH:mm"),
                r.AppliesToGroupTag
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, RestrictedTimeWindowRules = projected },
            $"Found {projected.Count} restricted time window rule(s).");
    }
}
