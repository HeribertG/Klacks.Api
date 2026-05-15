// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the descendant Shift ids reachable from a set of root shifts by
/// walking the Shift.OriginalId chain downward (root -> OriginalShift -> SplitShift).
/// The result maps each root id to the set of its descendant ids, optionally
/// including the root itself. Used by the order export pipeline to gather all
/// operational shifts that belong to a sealed order.
/// </summary>
namespace Klacks.Api.Application.Interfaces.Exports;

public interface IShiftDescendantResolver
{
    Task<Dictionary<Guid, HashSet<Guid>>> ResolveAsync(
        IReadOnlyCollection<Guid> rootIds,
        bool includeRoot,
        CancellationToken cancellationToken = default);
}
