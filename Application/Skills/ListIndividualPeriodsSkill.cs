// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the individual period schemes via ListQuery, each with its stretches, so the agent can pick
/// an individualPeriodId before update_individual_period / delete_individual_period.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_individual_periods")]
public class ListIndividualPeriodsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListIndividualPeriodsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var schemes = await _mediator.Send(new ListQuery<IndividualPeriodResource>(), cancellationToken);

        var projected = schemes
            .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
            .Select(s => new
            {
                s.Id,
                s.Name,
                Stretches = s.Periods
                    .OrderBy(p => p.FromDate)
                    .Select(p => new
                    {
                        p.Id,
                        FromDate = p.FromDate.ToString("yyyy-MM-dd"),
                        UntilDate = p.UntilDate?.ToString("yyyy-MM-dd"),
                        p.FullHours
                    })
                    .ToList()
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, IndividualPeriods = projected },
            $"Found {projected.Count} individual period scheme(s).");
    }
}
