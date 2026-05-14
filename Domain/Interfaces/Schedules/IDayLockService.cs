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
}
