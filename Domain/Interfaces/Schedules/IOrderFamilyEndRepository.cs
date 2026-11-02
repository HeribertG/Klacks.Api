// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Reads and locks a real shift family for setting its end date. Its head is the sealed order, or the row itself when it
/// stands alone without an order. Writes nothing itself: the caller changes the tracked rows and commits through the unit
/// of work.
/// </summary>
public interface IOrderFamilyEndRepository
{
    /// <summary>
    /// Locks the head row until the running transaction ends, so a second writer waits and reads the first
    /// one's result. Needs an open transaction.
    /// </summary>
    /// <param name="headId">Id of the head row</param>
    /// <param name="cancellationToken">Cancels the wait for the lock</param>
    Task LockHeadAsync(Guid headId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The live real rows of the family, tracked: the head and every row whose OriginalId points at it.
    /// </summary>
    /// <param name="headId">Id of the head row</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<List<Shift>> GetTrackedFamilyAsync(Guid headId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The date of the last real work after the given day on any row of the family, the head included, or null when there
    /// is none. A work on the day itself does not count.
    /// </summary>
    /// <param name="headId">Id of the head row</param>
    /// <param name="afterDate">The day after which works are looked for</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<DateOnly?> FindLastWorkDateAfterAsync(Guid headId, DateOnly afterDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// The date of the last not deleted work after the given day on a live scenario clone (ScenarioSourceShiftId) of any
    /// row of the family, or null when there is none. A clone of a clone carries the real root, so one step is enough. Real
    /// works do not count, see FindLastWorkDateAfterAsync.
    /// </summary>
    /// <param name="headId">Id of the head row</param>
    /// <param name="afterDate">The day after which works are looked for</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<DateOnly?> FindLastScenarioWorkDateAfterAsync(Guid headId, DateOnly afterDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// What the given shifts are still used by, with the same rule as the delete of a whole family.
    /// </summary>
    /// <param name="shiftIds">Ids of the shifts</param>
    /// <param name="cancellationToken">Cancels the queries</param>
    Task<OrderFamilyUsage> GetUsageAsync(IReadOnlyList<Guid> shiftIds, CancellationToken cancellationToken = default);
}
