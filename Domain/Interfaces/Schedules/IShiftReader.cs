// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Read-only, materialising shift reads of the Application and Domain layers: the real-plan scans of the proactive
/// detectors, the order lineage of the cut skills, the group read of find_split_shift_candidates, Shifts/ByIds and the
/// candidate tasks of a container template slot. Every read
/// keeps the shift base order (original id, from date, start time) unless it says otherwise and loads untracked.
/// </summary>
public interface IShiftReader
{
    /// <summary>
    /// The real-plan shifts the scan selects, untracked, in the base order (original id, from date, start time)
    /// unless the scan asks for from-date order; the scan's cap is applied in the database after that order.
    /// </summary>
    /// <param name="scan">Status, type, date, id and order restrictions</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<Shift>> ScanRealPlanAsync(RealPlanShiftScan scan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Id and from date of the real-plan shifts the scan selects, in the same order and with the same cap as
    /// ScanRealPlanAsync.
    /// </summary>
    /// <param name="scan">Status, type, date, id and order restrictions</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<ShiftDateKey>> ScanRealPlanKeysAsync(RealPlanShiftScan scan, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first shift with this status whose OriginalId is the given order, untracked, in the base order
    /// (original id, from date, start time); null when there is none. Scenario rows are not excluded.
    /// </summary>
    /// <param name="originalId">Id of the order the shift descends from (usually the sealed order)</param>
    /// <param name="status">Status the shift must have</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<Shift?> FindFirstByOriginalIdAsync(Guid originalId, ShiftStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many shifts with this status carry the given order as OriginalId. Scenario rows are not excluded.
    /// </summary>
    /// <param name="originalId">Id of the order the shifts descend from</param>
    /// <param name="status">Status the shifts must have</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<int> CountByOriginalIdAsync(Guid originalId, ShiftStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether any shift with this status carries the given order as OriginalId. Scenario rows are not excluded.
    /// </summary>
    /// <param name="originalId">Id of the order the shifts descend from</param>
    /// <param name="status">Status the shifts must have</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<bool> AnyByOriginalIdAsync(Guid originalId, ShiftStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sealed orders, plannable shifts and split parts with a live link to the group, projected to the columns
    /// find_split_shift_candidates reads, in the repository order (original id, from date, start time). Scenario rows
    /// are not excluded.
    /// </summary>
    /// <param name="groupId">Group the shifts must be linked to</param>
    /// <param name="nameFragment">Case-insensitive ILike fragment of the shift name (not escaped, so % and _ act as
    /// wildcards); null or blank = every name</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<OrderFamilyShiftRow>> GetOrderFamilyShiftsInGroupAsync(
        Guid groupId, string? nameFragment, CancellationToken cancellationToken = default);

    /// <summary>
    /// How many real-plan orders (task orders, no scenario rows, not deleted) carry no live group link, split into
    /// unsealed and sealed orders. Group-less orders are visible to every caller (ShiftGroupVisibilityFilter), so the
    /// counts need no caller restriction.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<OrderStatusCounts> CountUngroupedOrdersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The candidate tasks of a container template slot (see AvailableTaskCandidateCriteria), untracked, with their
    /// client and all the client's addresses, ordered by start time, then client name (client-less tasks last in
    /// PostgreSQL).
    /// </summary>
    /// <param name="criteria">Slot time frame and days, excluded and allowed shifts, search fragment</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<Shift>> GetAvailableTaskCandidatesAsync(
        AvailableTaskCandidateCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// The validity period (from date, until date) of the shift, read as an untracked projection; null when the shift
    /// does not exist or is soft-deleted.
    /// </summary>
    /// <param name="shiftId">Shift whose period is read</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<ShiftValidityPeriod?> GetValidityPeriodAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The given shifts that are tasks, whatever their status, sporadic flag or time, untracked, with their client and
    /// all the client's addresses, in the base order (original id, from date, start time). Unknown ids and non-task
    /// shifts are dropped; scenario rows are not excluded.
    /// </summary>
    /// <param name="shiftIds">Requested shift ids</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<Shift>> GetTasksWithClientByIdsAsync(
        IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The given real-plan shifts (no scenario copy, no scenario source link) the caller may see, untracked, with
    /// their client and the client's addresses, in the base order (original id, from date, start time).
    /// </summary>
    /// <param name="shiftIds">Requested shift ids; unknown ids are dropped</param>
    /// <param name="visibleGroupIds">The caller's restriction from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync
    /// (null = unrestricted, empty = only group-less shifts); deliberately not optional, like for GetFilteredAndPaginatedShifts</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<Shift>> GetVisibleRealPlanWithClientByIdsAsync(
        IReadOnlyCollection<Guid> shiftIds,
        IReadOnlyCollection<Guid>? visibleGroupIds,
        CancellationToken cancellationToken = default);
}
