// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// The admin override that recalculates frozen (sealed and exported) pay periods: requires a reason and records every
/// use in the period audit log (PeriodAuditAction.RecalculateExportedPeriod).
/// </summary>
public interface IFrozenPeriodOverrideAudit
{
    /// <summary>
    /// Throws InvalidRequestException when the override is requested without a non-blank reason or with a reason
    /// longer than PeriodAuditLimits.ReasonMaxLength, so the request fails before anything is recalculated.
    /// </summary>
    void EnsureReason(string? reason);

    /// <summary>
    /// Writes and commits one audit row for the recalculated window.
    /// </summary>
    /// <param name="startDate">First day of the requested window</param>
    /// <param name="endDate">Last day of the requested window</param>
    /// <param name="groupId">Group the recalculation was scoped to, null for all clients</param>
    /// <param name="reason">The admin's reason, trimmed before it is stored</param>
    /// <param name="frozenClientCount">Number of distinct clients with a frozen period in the window</param>
    Task RecordAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId,
        string reason,
        int frozenClientCount,
        CancellationToken cancellationToken);
}
