// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    Task<List<Work>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate, Guid? analyseToken = null, CancellationToken cancellationToken = default);
    Task<List<Work>> GetByClientAndDateRangeAsync(Guid clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default);
    Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default);
    Task<int> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default);

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
}
