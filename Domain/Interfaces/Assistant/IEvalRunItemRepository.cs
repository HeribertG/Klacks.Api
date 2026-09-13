// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the per-item breakdown of an eval run. Self-committing, and deliberately batched:
/// a full run writes one row per goldset item, so a per-row save would turn every run into hundreds of
/// round trips.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEvalRunItemRepository
{
    Task AddRangeAsync(IReadOnlyList<EvalRunItem> items, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvalRunItem>> ListByRunAsync(Guid evalRunId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The pure selection misses of one run that no proposal has spent yet: the expected tool WAS in the
    /// toolset and the model picked another. Retrieval misses are excluded on purpose - a tighter
    /// description cannot fix a tool that was never offered, and treating both as one number is what made
    /// ToolAccuracy unactionable. Every condition a caller has is a condition of the query, never a filter
    /// applied to the result: a limit that returns rows the caller must discard leaves the window occupied
    /// by rows nobody can ever spend, and the tail is then unreachable for good.
    /// </summary>
    /// <param name="evalRunId">The run whose per-item rows are read</param>
    /// <param name="itemIds">The goldset item ids the caller may learn from; an empty set returns nothing</param>
    /// <param name="limit">Safety ceiling on the rows read, expected to exceed the goldset size</param>
    Task<IReadOnlyList<EvalRunItem>> ListUnconsumedSelectionMissesAsync(
        Guid evalRunId,
        IReadOnlyCollection<string> itemIds,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stamps the consumption watermark so the rows of one run cannot justify a second narrowing while
    /// that run is the one the optimizer reads. The stop is per row, not per goldset item: a later full
    /// run writes fresh rows for the same items, so a miss that survives is offered again then.
    /// </summary>
    Task MarkConsumedAsync(
        IReadOnlyList<Guid> ids, DateTime consumedAtUtc, CancellationToken cancellationToken = default);
}
