// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Server-side mirror of the cells the schedule grid locks for an employee: a day outside the employee's membership
/// or outside the group period of the groups the grid shows cannot be planned. Used by the reassign path (REST and
/// skill), so the grid's drop and that path refuse exactly the same cells.
/// </summary>
public interface IClientPlanningPeriodGuard
{
    /// <summary>
    /// Throws InvalidRequestException naming the reason and the earliest offending day when any cell lies outside the
    /// planning period of its employee. An employee without a membership row and without group bounds is not blocked,
    /// like in the grid.
    /// </summary>
    /// <param name="cells">The employee and day pairs a write is about to occupy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureWithinPeriodsAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        CancellationToken cancellationToken);
}
