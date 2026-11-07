// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Server-side mirror of the cells the schedule grid locks for an employee: a day outside the employee's membership
/// or outside the group period of the groups the grid shows cannot be planned. Used by the reassign path, the Work
/// create paths (single POST, bulk add, place_work), a PUT that moves a Work to another cell, and as a per-placement
/// filter by propose_plan and the wizard apply, so the grid and those paths refuse exactly the same cells.
/// Deliberate exceptions that write without this check: replacements that set ReplaceClientId on a WorkChange
/// (CoverAbsenceCommandHandler, AddWorkChangeSkill, the WorkChange PUT) - the original Work stays with its employee;
/// the promotion of an accepted scenario into the real plan, whose cells were filtered only when the scenario was
/// written by a guarded path. Known open gap, not an exception by design: the harmonizer (and the recovery engine) move
/// Works onto other employees and days - by re-pointing cloned Works and by recreating uncorrelated ones from the
/// bitmap - without this check. The harmonizer models the membership and, when its request names a group (the
/// AutoWizard chain and the start_wizard skills), the period in that group; a run started from the harmonizer
/// controllers has no group, and the recovery engine was not part of that change. Such runs can still leave, and on accept promote, Works outside an employee's group period.
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

    /// <summary>
    /// Returns every cell outside the planning period of its employee, earliest day first, one violation per cell
    /// (the membership reason wins over the group period reason), without throwing.
    /// </summary>
    /// <param name="cells">The employee and day pairs a write is about to occupy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<PlanningPeriodViolation>> FindViolationsAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        CancellationToken cancellationToken);

    /// <summary>
    /// Same as FindViolationsAsync, but the group period is read for the given groups (the target group of a planner
    /// run or proposal, already checked for visibility by the caller) instead of the request's group context; an empty
    /// list checks the membership only.
    /// </summary>
    /// <param name="cells">The employee and day pairs a write is about to occupy</param>
    /// <param name="groupIds">The groups whose period bounds apply</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<PlanningPeriodViolation>> FindViolationsInGroupsAsync(
        IReadOnlyCollection<(Guid ClientId, DateOnly Date)> cells,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refuses targets that cannot take over a top-level Work at all, whatever the day, in one query: a client that does
    /// not exist or is deleted reads like a missing client (KeyNotFoundException); a customer is refused
    /// (InvalidRequestException), only an employee, an external employee or a stand-in can be planned
    /// (ClientTypeRules.IsPlannable).
    /// Visibility stays the caller's check. Services inside a container may belong to a customer and are not checked here.
    /// </summary>
    /// <param name="clientIds">The clients Works are about to be given to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task EnsurePlannablePersonsAsync(IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken);
}
