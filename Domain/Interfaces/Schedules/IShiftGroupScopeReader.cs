// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Batch lookup of the groups a shift belongs to, used to narrow the audience of a proactive finding
/// to the planners who are allowed to see that shift. A shift is a member of MANY groups at once
/// (GroupItem is a many-to-many join), so a caller keeping only one of them would silently drop the
/// planners of every other group; both methods therefore return all of them. Batched by design: the
/// callers hold up to a thousand candidate ids per scan and must never issue one query per id.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftGroupScopeReader
{
    /// <summary>
    /// Groups per shift id, each list ordered by group id so a caller deriving a single representative
    /// value from it (a navigation hint, the ledger's single group column) gets the same one on every
    /// scan. A shift id without any live group membership is absent from the result rather than mapped
    /// to an empty list.
    /// </summary>
    /// <param name="shiftIds">The shift ids to resolve; duplicates are collapsed.</param>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetGroupIdsByShiftIdsAsync(
        IReadOnlyCollection<Guid> shiftIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The same lookup keyed by Work id. Group membership is a property of the Work's Shift, never of
    /// the individual work entry, so a work id resolves through its shift in a single join.
    /// Soft-deleted Work rows are excluded, which is right for a work still referenced as live (a lock
    /// conflict names one in its error text) and WRONG for an entity the caller has just removed: an
    /// already-cancelled work resolves to nothing here and would be routed to Admins alone. A caller
    /// holding cancelled works must pass their ShiftIds to
    /// <see cref="GetGroupIdsByShiftIdsAsync"/> instead.
    /// </summary>
    /// <param name="workIds">The work ids to resolve; duplicates are collapsed.</param>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetGroupIdsByWorkIdsAsync(
        IReadOnlyCollection<Guid> workIds,
        CancellationToken cancellationToken = default);
}
