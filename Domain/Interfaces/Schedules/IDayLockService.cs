// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Domain guard that prevents writes to schedule entries on sealed days.
/// </summary>
public interface IDayLockService
{
    /// <summary>
    /// Throws InvalidRequestException if (date, clientId) is covered by a SealedDay row
    /// (global or via the client's group membership). Skips when analyseToken has a value
    /// because scenarios run in a sandbox that bypasses day-level seals.
    /// </summary>
    /// <param name="date">The CurrentDate of the entity to be written</param>
    /// <param name="clientId">The ClientId of the entity to be written</param>
    /// <param name="analyseToken">Scenario token; when not null the check is skipped</param>
    Task EnsureNotLockedAsync(DateOnly date, Guid clientId, Guid? analyseToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Same guard for a whole batch, resolved in one repository call. Entries carrying an analyse token
    /// are scenario writes and are skipped, exactly as in the single-entry check.
    /// </summary>
    /// <param name="entries">Entries the caller is about to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureNoneLockedAsync(
        IReadOnlyCollection<(DateOnly Date, Guid ClientId, Guid? AnalyseToken)> entries,
        CancellationToken cancellationToken = default);
}
