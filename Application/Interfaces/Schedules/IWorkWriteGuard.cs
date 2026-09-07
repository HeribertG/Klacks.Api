// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Pre-write checks a Work must pass before it is (re-)placed into the real plan: the sporadic-shift
/// capacity and the one conflict class that can never be reported after the fact. Shared by the create
/// and the restore path so both refuse exactly the same writes.
/// </summary>
public interface IWorkWriteGuard
{
    /// <summary>
    /// Throws ConflictException when the Work's shift is sporadic and its day or range capacity is used up.
    /// </summary>
    /// <param name="work">The Work about to be written; its own id is excluded from the usage count</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureNoSporadicConflictAsync(Work work, CancellationToken cancellationToken);

    /// <summary>
    /// Throws ConflictException when the Work would introduce a hard-blocking schedule conflict (a missing
    /// mandatory qualification). Skipped for scenario writes, mirroring the day-lock contract.
    /// </summary>
    /// <param name="work">The Work about to be written</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsureNoHardBlockingConflictAsync(Work work, CancellationToken cancellationToken);
}
