// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
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
    /// absence window plus context days on each side. With <paramref name="source"/> the source scenario owns the
    /// days of its range for its group's members and, for every other employee, the works on the shifts it cloned
    /// (scenario from scenario); everything else stays the real plan.
    /// </summary>
    /// <param name="groupId">The planning group whose members are the candidate pool</param>
    /// <param name="absentClientId">The agent that fell out</param>
    /// <param name="absenceDates">The days the agent is unavailable</param>
    /// <param name="source">Scenario the cover is built on; null = the real plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<RecoverySnapshot> BuildAsync(
        Guid groupId,
        Guid absentClientId,
        IReadOnlyList<DateOnly> absenceDates,
        ScenarioSourceScope? source,
        CancellationToken cancellationToken);
}
