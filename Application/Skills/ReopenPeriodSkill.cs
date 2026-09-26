// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Re-opens a sealed billing period — the same group-aware path as the period-closing page:
/// puts each sealed entry back to the LockLevel it had before the close (entries sealed before
/// that level was recorded go to None), removes the SealedDay day locks and writes the
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
using Klacks.Api.Domain.Models.Schedules;
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

        var result = await _mediator.Send(
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
        var entries = result.Entries;

        return SkillResult.SuccessResult(
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupId = groupId,
                GroupName = groupName,
                Reason = reason,
                AffectedItems = result.AffectedCount,
                ReopenedEntries = entries.Total,
                RestoredConfirmed = entries.RestoredConfirmed,
                RestoredApproved = entries.RestoredApproved,
                OpenAgain = entries.RestoredNone,
                WithoutRecordedLevel = entries.WithoutRecordedLevel,
                LiftedDayLocks = result.SealedDayCount
            },
            $"Re-opened period {startDate}..{endDate} for {scopeLabel}: day locks removed and confirmed in the " +
            $"database (verified). {DescribeEntries(entries)}Note: payroll exports already created for this " +
            "period may no longer match — re-seal after the correction.");
    }

    /// <summary>
    /// States what the reopen did with the sealed entries, so the answer never claims more than happened:
    /// restored Confirmed/Approved entries stay locked at that level, and entries without a recorded level
    /// lost whatever confirmation or approval they had before the close.
    /// </summary>
    private static string DescribeEntries(PeriodUnsealCounts entries)
    {
        var text = $"{entries.Total} work/break entry(ies) reopened: {entries.RestoredConfirmed} back to Confirmed and " +
            $"{entries.RestoredApproved} back to Approved as they were before the close (they stay locked at that level), " +
            $"{entries.RestoredNone} open again. ";

        if (entries.WithoutRecordedLevel > 0)
        {
            text += $"{entries.WithoutRecordedLevel} entry(ies) were sealed before the pre-close state was recorded and " +
                "are now open (None): a confirmation or approval they may have had before the close is NOT restored. ";
        }

        return text;
    }
}
