// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Read-only access to the inputs of company-holiday detection for one group subtree: who is a member when,
/// and who is away the whole day. Only the real plan is read - scenario rows are excluded.
/// </summary>
public interface IGroupAbsenceReadRepository
{
    /// <summary>
    /// Membership windows of every employee assigned to the root group or any group below it. A GroupItem
    /// counts when it is not deleted and belongs to the real plan (no AnalyseToken, no
    /// ScenarioSourceGroupItemId); windows that do not overlap the range are left out.
    /// </summary>
    /// <param name="rootId">Id of the root group whose subtree is read</param>
    /// <param name="from">First day of the range</param>
    /// <param name="until">Last day of the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<GroupMembershipWindow>> GetMembershipWindowsAsync(
        Guid rootId,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Days on which the given employees have a full-day absence booked (a real-plan Break whose times are
    /// the full-day marker 00:00-23:59).
    /// </summary>
    /// <param name="clientIds">The employees to read</param>
    /// <param name="from">First day of the range</param>
    /// <param name="until">Last day of the range</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<ClientFullDayAbsence>> GetFullDayAbsencesAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default);
}
