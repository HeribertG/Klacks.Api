// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Repository for SealedDay reads and writes.
/// </summary>
public interface ISealedDayRepository
{
    Task AddAsync(SealedDay entry, CancellationToken cancellationToken = default);

    /// <summary>Period seals (Level Closed) of the range, for the group and global ones; day approvals are excluded.</summary>
    Task<List<SealedDay>> GetRangeAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the period seals (Level Closed) of exactly this group (or the global ones); day approvals stay.</summary>
    Task<int> SoftDeleteRangeAsync(DateOnly from, DateOnly to, Guid? groupId, string deletedBy, CancellationToken cancellationToken = default);

    Task<bool> IsDayLockedAsync(DateOnly date, Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the date is sealed for a shift: globally, or by any group that contains the shift via a live
    /// GroupItem. Unlike <see cref="IsDayLockedAsync"/> this does not depend on the client having a live
    /// Work that day - the guard for restoring a deleted Work, whose client has no such Work by definition.
    /// </summary>
    /// <param name="date">Day to test.</param>
    /// <param name="shiftId">Shift the Work belongs to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> IsDayLockedForShiftAsync(DateOnly date, Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same rule as <see cref="IsDayLockedAsync"/> for many pairs at once, in two queries in total
    /// instead of two per pair. A bulk insert of a whole wizard result checks hundreds of pairs, which
    /// made the guard alone the dominant cost of the apply.
    /// </summary>
    /// <param name="pairs">Pairs of (date, client) to test.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The subset of <paramref name="pairs"/> that is sealed.</returns>
    Task<HashSet<(DateOnly Date, Guid ClientId)>> GetLockedPairsAsync(
        IReadOnlyCollection<(DateOnly Date, Guid ClientId)> pairs,
        CancellationToken cancellationToken = default);

    Task<DateOnly?> FindFirstLockedDateForClientAsync(DateOnly from, DateOnly to, Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every (client, date) of the range that is sealed for the client - the cells planners must treat as fixed.
    /// </summary>
    /// <param name="clientIds">Clients to test.</param>
    /// <param name="from">First day (inclusive).</param>
    /// <param name="until">Last day (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <summary>The day approvals (Level Approved rows) of a date, of every group.</summary>
    /// <param name="date">The approved day.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<SealedDay>> GetDayApprovalsAsync(DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes the day approval of exactly this group on the date; returns the number of rows.</summary>
    /// <param name="date">The approved day.</param>
    /// <param name="groupId">The approving group.</param>
    /// <param name="deletedBy">Who revokes the approval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int> SoftDeleteDayApprovalAsync(DateOnly date, Guid groupId, string deletedBy, CancellationToken cancellationToken = default);

    Task<HashSet<(Guid ClientId, DateOnly Date)>> GetLockedClientDaysAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default);
}
