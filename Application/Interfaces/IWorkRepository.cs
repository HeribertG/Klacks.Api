// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Interfaces;

public interface IWorkRepository : IBaseRepository<Work>
{
    /// <summary>
    /// The top-level works moved away from this person on this day in this scenario or the open scenario it was built on,
    /// untracked with a stable order. A work whose clone chain ends at a real-plan work with the same origin was moved by an
    /// earlier accept, it is left out.
    /// </summary>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="movedFromClientId">The person the works came from</param>
    /// <param name="date">Workday</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<Work>> ListMovedByScenarioAsync(
        Guid analyseToken, Guid movedFromClientId, DateOnly date, CancellationToken cancellationToken);

    /// <summary>
    /// Whether this person owns a work a cover moved in this scenario, on any day.
    /// </summary>
    /// <param name="analyseToken">Token of the scenario</param>
    /// <param name="clientId">The person</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> HoldsMovedWorkInScenarioAsync(Guid analyseToken, Guid clientId, CancellationToken cancellationToken);

    Task<List<Work>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate, Guid? analyseToken = null, CancellationToken cancellationToken = default);
    Task<List<Work>> GetByClientAndDateRangeAsync(Guid clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    /// <summary>
    /// Raises the lock level of the day's works of the group, but only of those whose service has ended by the cutoff:
    /// a service that is still to come or still running stays as it is.
    /// </summary>
    /// <param name="date">Workday</param>
    /// <param name="groupId">Group whose shifts the works belong to</param>
    /// <param name="level">Target lock level</param>
    /// <param name="sealedBy">Actor stamped into SealedBy</param>
    /// <param name="endedBy">Company-local now; only works ending at or before it are sealed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, ServiceEndCutoff endedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the group still has a work on the day whose service has not ended by the cutoff and that is below the level.
    /// </summary>
    /// <param name="date">Workday</param>
    /// <param name="groupId">Group whose shifts the works belong to</param>
    /// <param name="level">Lock level the day is raised to</param>
    /// <param name="endedBy">Company-local now</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<bool> HasUnendedByDayAndGroupAsync(DateOnly date, Guid groupId, WorkLockLevel level, ServiceEndCutoff endedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of top-level works of the real plan in the period that are still below Confirmed. Works of a soft-deleted person do not count: nobody can confirm them any more, and the close seals them together with the rest. With a group only the
    /// works on the group's own shifts count - the same population SealByPeriodAndGroup seals - so works of other
    /// groups never block this group's close; without a group every work of the period counts.
    /// </summary>
    /// <param name="startDate">First day of the period</param>
    /// <param name="endDate">Last day of the period</param>
    /// <param name="groupId">Group of a group close, null for a whole-period close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> CountUnsealedForCloseAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first unsealed works counted by CountUnsealedForCloseAsync, ordered by day, start time and id.
    /// </summary>
    /// <param name="startDate">First day of the period</param>
    /// <param name="endDate">Last day of the period</param>
    /// <param name="groupId">Group of a group close, null for a whole-period close</param>
    /// <param name="limit">Maximum number of rows returned</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<UnsealedWorkDto>> ListUnsealedForCloseAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of top-level works of the real plan in the period that lie on a stand-in (ClientTypeRules.StandInTypes),
    /// whatever their lock level, over the population the close seals (with a group only the works on the group's own
    /// shifts). Unlike CountUnsealedForCloseAsync, the works of a soft-deleted stand-in count too: the close would seal
    /// them as well.
    /// </summary>
    /// <param name="startDate">First day of the period</param>
    /// <param name="endDate">Last day of the period</param>
    /// <param name="groupId">Group of a group close, null for a whole-period close</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> CountStandInWorksForCloseAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);
    Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<PeriodUnsealCounts> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<PeriodUnsealCounts> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);

    Task<List<(DateOnly Date, int Total, int Sealed)>> GetSealingSummaryAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default);

    Task<List<UsedPeriodDto>> GetUsedPeriodsAsync(CancellationToken cancellationToken = default);

    Task<SporadicCapacityUsage> GetSporadicCapacityUsageAsync(
        Guid shiftId,
        DateOnly bookingDate,
        DateOnly rangeFrom,
        DateOnly rangeUntil,
        Guid? excludeWorkId,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);

    Task<List<Work>> GetFutureUnlockedByShiftIdsAsync(IEnumerable<Guid> shiftIds, DateOnly fromDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether the shift has any non-scenario Work row whose LockLevel is above None - the
    /// item-level seal signal QuietWindowService checks before proposing a remediation for it.
    /// </summary>
    /// <param name="shiftId">Shift whose Work rows are checked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> HasLockedWorkForShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a soft-deleted Work past the global query filter. Returns null for an unknown id and for a
    /// Work that is not deleted, so the caller can treat both as "nothing to restore".
    /// </summary>
    /// <param name="id">Work id.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Work?> GetDeletedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the soft-delete stamp of a tracked Work and re-runs the work macro, exactly like Add and Put
    /// do, so surcharges and overtime reflect the plan as it is now. Stage-only; the caller commits.
    /// </summary>
    /// <param name="work">The tracked, soft-deleted Work.</param>
    Task RestoreAsync(Work work);

    /// <summary>
    /// The audit actor of the most recent touch (create, update or soft delete) on any non-scenario Work
    /// under this shift, soft-deleted rows included - for an empty container the last person who removed an
    /// assignment IS the last planner, and a live-rows-only query would never find them. Returns the raw
    /// audit string (a user id, or the Anonymous/System sentinels) or null when no Work was ever recorded;
    /// the caller decides what counts as a person. No tracking.
    /// </summary>
    /// <param name="shiftId">Shift whose Work rows are inspected.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<string?> GetLastPlannerAuditActorForShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
}
