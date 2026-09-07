// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Repository for SealedDay reads and writes.
/// </summary>
public interface ISealedDayRepository
{
    Task AddAsync(SealedDay entry, CancellationToken cancellationToken = default);

    Task<List<SealedDay>> GetRangeAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default);

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
}
