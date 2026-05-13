// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that revokes day approval — drops Approved (2) back to Confirmed (1) for all works of the given
/// group on the given date. Wraps RevokeDayApprovalCommand.
/// </summary>
/// <param name="date">Workday in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Group UUID.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("revoke_day_approval")]
public class RevokeDayApprovalSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public RevokeDayApprovalSkill(IMediator mediator)
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
        var count = await _mediator.Send(new RevokeDayApprovalCommand(date, groupId), cancellationToken);
        return SkillResult.SuccessResult(
            new { Date = date, GroupId = groupId, AffectedWorks = count },
            $"Revoked approval on {count} work(s) on {date} for group {groupId}. LockLevel dropped to Confirmed.");
    }
}
