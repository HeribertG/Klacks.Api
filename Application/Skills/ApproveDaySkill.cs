// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that approves all works for a given day and group — sets LockLevel to Approved (2) so the
/// schedule cells become read-only beyond Confirmed. Returns the number of affected work entries.
/// Wraps ApproveDayCommand.
/// </summary>
/// <param name="date">Workday in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Group UUID whose clients' works on that date should be approved.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("approve_day")]
public class ApproveDaySkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ApproveDaySkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var groupId = GetRequiredGuid(parameters, "groupId");
        var count = await _mediator.Send(new ApproveDayCommand(date, groupId), cancellationToken);
        return SkillResult.SuccessResult(
            new { Date = date, GroupId = groupId, AffectedWorks = count },
            $"Approved {count} work(s) on {date} for group {groupId}. LockLevel raised to Approved.");
    }
}
