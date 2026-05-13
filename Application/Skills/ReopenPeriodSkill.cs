// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that re-opens a closed payment period — drops Closed back so the works become editable again.
/// Wraps ReopenPeriodCommand.
/// </summary>
/// <param name="startDate">Period start (inclusive).</param>
/// <param name="endDate">Period end (inclusive).</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reopen_period")]
public class ReopenPeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ReopenPeriodSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");
        var count = await _mediator.Send(new ReopenPeriodCommand(startDate, endDate), cancellationToken);
        return SkillResult.SuccessResult(
            new { StartDate = startDate, EndDate = endDate, AffectedWorks = count },
            $"Re-opened period {startDate}..{endDate}: {count} work(s) returned to editable state.");
    }
}
