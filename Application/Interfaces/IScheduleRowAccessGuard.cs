// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Single decision whether the caller may read or write a schedule row (work, break, expense, work change, note,
/// command) addressed by its id: the owning client must be visible (IClientVisibilityGuard) AND the scenario the row
/// lives in must be visible (IScenarioTokenAccessGuard; null token = main plan). By-id handlers take the token from the
/// STORED row or its loaded parent Work, never from the request, because the request carries no token for a by-id
/// GET/DELETE and a forged one for a PUT. A hidden row must be answered exactly like a missing one.
/// </summary>
public interface IScheduleRowAccessGuard
{
    /// <param name="clientId">Owning client of the row</param>
    /// <param name="analyseToken">Scenario token of the stored row (or its parent Work); null for the main plan</param>
    Task<bool> IsVisibleAsync(Guid clientId, Guid? analyseToken, CancellationToken cancellationToken = default);

    /// <param name="clientIds">Owning client ids of the rows a command touches (old and new owner)</param>
    /// <param name="analyseToken">Scenario token of the stored row (or its parent Work); null for the main plan</param>
    Task<bool> AreAllVisibleAsync(
        IReadOnlyCollection<Guid> clientIds, Guid? analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps only the rows whose owning client and stored scenario are visible, preserving their order (bulk by-id
    /// writes treat the others like missing ids).
    /// </summary>
    /// <param name="items">Loaded rows</param>
    /// <param name="clientIdOf">Selects the owning client id of a row</param>
    /// <param name="analyseTokenOf">Selects the stored scenario token of a row; null for the main plan</param>
    Task<List<T>> FilterVisibleAsync<T>(
        IReadOnlyCollection<T> items,
        Func<T, Guid> clientIdOf,
        Func<T, Guid?> analyseTokenOf,
        CancellationToken cancellationToken = default);
}
