// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The close gates shared by PeriodAutoCloseService and evaluate_period_close_readiness, so that the automatic close
/// and the advice for a person cannot drift apart: which period of a group is the candidate, the ordered check of its
/// state, the sealed-state reads and the error count. FirstStopAsync short-circuits on purpose, each read only happens
/// when the gate before it let the period through. The autonomy gate is not part of this class: it decides whether
/// Klacksy may act alone and says nothing about a person closing the period.
/// </summary>
/// <param name="group">The group the period belongs to</param>
/// <param name="staffing">Tells whether the group or a descendant holds clients or shifts</param>
/// <param name="referenceDay">The period must have ended strictly before this day</param>
/// <param name="referenceWeekStart">First day of the configured week that contains the reference day</param>
/// <param name="candidate">The period under evaluation</param>

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public static class PeriodCloseGates
{
    /// <summary>
    /// The latest period of the group that ended before the reference day, or null for an Individual group (no
    /// derivable cycle), an unstaffed group and a period ending before the group's ValidFrom.
    /// </summary>
    public static PeriodCloseCandidate? FindCandidate(
        Group group, GroupStaffingLookup staffing, DateOnly referenceDay, DateOnly referenceWeekStart)
    {
        if (!NextPeriodBoundaries.HasDerivableCycle(group.PaymentInterval) || !staffing.IsStaffed(group.Id))
        {
            return null;
        }

        var periodEnd = PeriodBoundaries.LastEndBefore(group, referenceDay, referenceWeekStart);
        if (periodEnd < DateOnly.FromDateTime(group.ValidFrom))
        {
            return null;
        }

        return new PeriodCloseCandidate(group, PeriodBoundaries.StartFor(group.PaymentInterval, periodEnd), periodEnd);
    }

    public static async Task<PeriodCloseStop> FirstStopAsync(
        ISealedDayRepository sealedDayRepository,
        IScheduleActivityProbe activityProbe,
        IPeriodAuditLogRepository auditLogRepository,
        PeriodCloseCandidate candidate,
        CancellationToken cancellationToken)
    {
        if (await IsLastDaySealedAsync(sealedDayRepository, candidate, cancellationToken))
        {
            return PeriodCloseStop.LastDaySealed;
        }

        if (!await activityProbe.HasDirectWorkInRangeAsync(
                candidate.Group, candidate.PeriodStart, candidate.PeriodEnd, cancellationToken))
        {
            return PeriodCloseStop.NoDirectWork;
        }

        return await HasReopenHistoryAsync(auditLogRepository, candidate, cancellationToken)
            ? PeriodCloseStop.ReopenedBefore
            : PeriodCloseStop.None;
    }

    /// <summary>
    /// Whether anybody ever REOPENED the period - an unseal entry of this group or installation-wide overlapping
    /// it. A seal entry alone does not count: a partial seal still holds its day locks.
    /// </summary>
    public static async Task<bool> HasReopenHistoryAsync(
        IPeriodAuditLogRepository repository, PeriodCloseCandidate candidate, CancellationToken cancellationToken)
    {
        var entries = await repository.GetRangeAsync(candidate.PeriodStart, candidate.PeriodEnd, cancellationToken);
        return entries.Any(entry =>
            entry.Action == PeriodAuditAction.Unseal
            && (entry.GroupId == null || entry.GroupId == candidate.Group.Id));
    }

    public static async Task<bool> IsLastDaySealedAsync(
        ISealedDayRepository repository, PeriodCloseCandidate candidate, CancellationToken cancellationToken)
    {
        var sealedDays = await repository.GetRangeAsync(
            candidate.PeriodEnd, candidate.PeriodEnd, candidate.Group.Id, cancellationToken);
        return sealedDays.Count > 0;
    }

    public static async Task<bool> HasAnySealedDayAsync(
        ISealedDayRepository repository, PeriodCloseCandidate candidate, CancellationToken cancellationToken)
    {
        var sealedDays = await repository.GetRangeAsync(
            candidate.PeriodStart, candidate.PeriodEnd, candidate.Group.Id, cancellationToken);
        return sealedDays.Count > 0;
    }

    public static int CountErrors(IEnumerable<PeriodIssueDto> issues) =>
        issues.Count(issue => issue.Severity == ScheduleValidationType.Error);
}
