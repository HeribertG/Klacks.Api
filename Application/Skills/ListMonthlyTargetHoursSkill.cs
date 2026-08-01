// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the company-wide monthly target hours rows via ListQuery, ordered by year and month, so the
/// agent can pick a row id before update_monthly_target_hours / delete_monthly_target_hours.
/// </summary>
/// <param name="year">Optional year filter; without it every defined month is returned.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_monthly_target_hours")]
public class ListMonthlyTargetHoursSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListMonthlyTargetHoursSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rows = await _mediator.Send(new ListQuery<MonthlyTargetHoursResource>(), cancellationToken);

        var year = GetParameter<int?>(parameters, "year");
        if (year.HasValue)
        {
            rows = rows.Where(r => r.Year == year.Value);
        }

        var projected = rows
            .OrderBy(r => r.Year)
            .ThenBy(r => r.Month)
            .Select(r => new { r.Id, r.Year, r.Month, r.Hours })
            .ToList();

        var scope = year.HasValue ? $" for {year.Value}" : string.Empty;

        return SkillResult.SuccessResult(
            new { Count = projected.Count, MonthlyTargetHours = projected },
            $"Found {projected.Count} monthly target hours row(s){scope}.");
    }
}
