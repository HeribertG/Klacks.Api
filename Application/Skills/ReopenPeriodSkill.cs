// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Re-opens a sealed billing period — the same group-aware path as the period-closing page:
/// drops the LockLevel back to editable, removes the SealedDay day locks and writes the
/// period audit log with the MANDATORY reason (as the UI enforces). The group can be
/// addressed by UUID or by name; without a group the unseal is global. Warns that existing
/// payroll exports of the period may no longer match after the re-open. Verified by
/// re-reading the day-lock state afterwards.
/// </summary>
/// <param name="startDate">Period start (inclusive).</param>
/// <param name="endDate">Period end (inclusive).</param>
/// <param name="reason">Required. Why the period is re-opened; stored in the audit log.</param>
/// <param name="groupId">Optional. UUID of the group; takes precedence over groupName.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reopen_period")]
public class ReopenPeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;

    public ReopenPeriodSkill(
        IMediator mediator,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard)
    {
        _mediator = mediator;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
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

        var reason = GetParameter<string>(parameters, "reason");
        if (string.IsNullOrWhiteSpace(reason))
        {
            return SkillResult.Error(
                "reason is required: re-opening a sealed period must be justified for the audit log — " +
                "ask the user why the period should be re-opened.");
        }

        var (groupId, groupName, groupError) = await PeriodClosingGroupResolver.ResolveAsync(
            GetParameter<string>(parameters, "groupId"),
            GetParameter<string>(parameters, "groupName"),
            _groupRepository, _groupScopeGuard, context, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var count = await _mediator.Send(
            new ReopenPeriodByGroupCommand(startDate, endDate, groupId, reason.Trim()), cancellationToken);

        var days = await _mediator.Send(
            new GetSealedPeriodsQuery(startDate, endDate, groupId), cancellationToken);
        var stillSealed = days.Count(d => d.IsDaySealed);
        if (stillSealed > 0)
        {
            return SkillResult.Error(
                $"Database verification failed: {stillSealed} day(s) in {startDate}..{endDate} still carry a day " +
                "lock after the re-open — treat the period as still sealed.");
        }

        var scopeLabel = groupId.HasValue ? $"group '{groupName}'" : "ALL groups (global)";

        return SkillResult.SuccessResult(
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupId = groupId,
                GroupName = groupName,
                Reason = reason,
                AffectedItems = count
            },
            $"Re-opened period {startDate}..{endDate} for {scopeLabel}: {count} item(s) editable again, day locks " +
            "removed and confirmed in the database (verified). Note: payroll exports already created for this " +
            "period may no longer match — re-seal after the correction.");
    }
}
