// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// Assembles the immutable, I/O-free <see cref="RecoverySnapshot"/> the pure recovery engine consumes,
/// by reading the live plan, the group's members, their contract constraints, fairness signals,
/// preferences, qualification gates and availability over a context window around the absence.
/// </summary>
public interface IRecoverySnapshotBuilder
{
    /// <summary>
    /// Builds a snapshot for the given group and absence. The candidate pool is the group's active
    /// members in stable Guid order; the plan is read from the real schedule (token-blind) over the
    /// absence window plus context days on each side.
    /// </summary>
    /// <param name="groupId">The planning group whose members are the candidate pool</param>
    /// <param name="absentClientId">The agent that fell out</param>
    /// <param name="absenceDates">The days the agent is unavailable</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<RecoverySnapshot> BuildAsync(
        Guid groupId,
        Guid absentClientId,
        IReadOnlyList<DateOnly> absenceDates,
        CancellationToken cancellationToken);
}
