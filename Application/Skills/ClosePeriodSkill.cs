// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Seals a billing period — the same group-aware path as the period-closing page: sets
/// LockLevel Closed on the works/breaks in range, creates the authoritative SealedDay
/// day locks and writes the period audit log. Without a group the seal is global. A seal
/// starts no payroll export; the person-based payroll export is run separately on the
/// period-closing page once every day of the period is locked. The group can be addressed
/// by UUID or by name. The seal is verified by re-reading the day-lock state afterwards.
/// </summary>
/// <param name="startDate">Period start in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="endDate">Period end in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="groupId">Optional. UUID of the group to seal; takes precedence over groupName.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>
/// <param name="reason">Optional. Free-text reason stored in the period audit log.</param>

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("close_period")]
public class ClosePeriodSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;

    public ClosePeriodSkill(
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

        var (groupId, groupName, groupError) = await PeriodClosingGroupResolver.ResolveAsync(
            GetParameter<string>(parameters, "groupId"),
            GetParameter<string>(parameters, "groupName"),
            _groupRepository, _groupScopeGuard, context, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var reason = GetParameter<string>(parameters, "reason");

        var count = await _mediator.Send(
            new ClosePeriodByGroupCommand(startDate, endDate, groupId, reason), cancellationToken);

        var days = await _mediator.Send(
            new GetSealedPeriodsQuery(startDate, endDate, groupId), cancellationToken);
        var sealedDays = days.Count(d => d.IsDaySealed);
        if (sealedDays == 0)
        {
            return SkillResult.Error(
                $"Database verification failed: no day locks found for {startDate}..{endDate} after the seal — " +
                "treat the period as not sealed.");
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
                AffectedItems = count,
                SealedDays = sealedDays
            },
            $"Sealed period {startDate}..{endDate} for {scopeLabel}: {count} item(s) locked, {sealedDays} day lock(s) " +
            "confirmed in the database (verified).");
    }
}
