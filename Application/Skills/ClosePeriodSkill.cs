// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that closes a payment period — sets LockLevel to Closed (3) across all works in the range so
/// nothing (not even confirmed/approved) can change anymore. Returns the number of affected work entries.
/// Wraps ClosePeriodCommand.
/// </summary>
/// <param name="startDate">Period start in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="endDate">Period end in ISO yyyy-MM-dd (inclusive).</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("close_period")]
public class ClosePeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ClosePeriodSkill(IMediator mediator)
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
        if (startDate > endDate)
        {
            return SkillResult.Error($"startDate ({startDate}) must be on or before endDate ({endDate}).");
        }
        var count = await _mediator.Send(new ClosePeriodCommand(startDate, endDate), cancellationToken);
        return SkillResult.SuccessResult(
            new { StartDate = startDate, EndDate = endDate, AffectedWorks = count },
            $"Closed period {startDate}..{endDate}: {count} work(s) sealed at LockLevel Closed.");
    }
}
