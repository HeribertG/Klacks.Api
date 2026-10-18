// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftRepository : IBaseRepository<Shift>
{
    new Task<Shift?> Get(Guid id);

    Task<Shift?> GetTrackedOrFromDb(Guid id);

    /// <summary>
    /// Update from the cut dialog: scalars and group items only. The cut list hands the dialog no default
    /// expenses and no required qualifications, so a full update would delete both; this variant leaves them as
    /// stored. The edit form keeps using Put, which owns both lists. The returned shift carries both lists as
    /// stored, so a response mapped from it shows them. Like Put, it keeps the ERP reference and the scenario
    /// clone tracking, which the resource does not carry.
    /// </summary>
    /// <param name="shift">The shift as sent by the cut dialog, with the id of the stored shift.</param>
    Task<Shift?> PutCutUpdate(Shift shift);

    IQueryable<Shift> GetQuery();
   
    IQueryable<Shift> GetQueryWithClient();

    /// <summary>
    /// Filters shifts by the given criteria. <paramref name="today"/> is the company's own local day
    /// (per ICompanyClock), resolved by the caller since this method itself is synchronous.
    /// <paramref name="visibleGroupIds"/> is the caller's group restriction from
    /// IShiftGroupFilterService.ResolveShiftListGroupIdsAsync (null = unrestricted, empty = only group-less
    /// shifts); it is deliberately not optional so no caller can forget the security boundary.
    /// </summary>
    IQueryable<Shift> FilterShifts(ShiftFilter filter, DateOnly today, IReadOnlyCollection<Guid>? visibleGroupIds);

    Task<TruncatedShift> GetFilteredAndPaginatedShifts(ShiftFilter filter, IReadOnlyCollection<Guid>? visibleGroupIds);

    Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds);
    
    Task<List<Group>> GetGroupsForShift(Guid shiftId);

    Task<List<Shift>> CutList(Guid id, DateOnly? filterClosedBefore = null, bool tracked = false);

    Task<Shift?> GetSealedOrder(Guid originalId);

    Task<TruncatedShift> GetPaginatedShifts(IQueryable<Shift> filteredQuery, ShiftFilter filter);

    Task<Shift> AddWithSealedOrderHandling(Shift shift);

    Task<List<Shift>> GetOpenOrdersAsync(OpenOrderFilter filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every non-scenario sealed order, plannable shift and split shift, untracked, with its
    /// customer (addresses and group memberships) and its own group links.
    /// </summary>
    /// <param name="customerName">Case-insensitive fragment the customer's name or company must contain; null for every customer.</param>
    Task<List<Shift>> GetShiftsForCityGroupPlacementAsync(
        string? customerName, CancellationToken cancellationToken = default);

    Task<Shift?> FindReusableUncutOrderAsync(Shift candidate, CancellationToken cancellationToken = default);

    Task<Shift?> FindActiveByExternalReferenceAsync(string sourceSystemId, string externalOrderReference, CancellationToken cancellationToken = default);

    Task<Shift?> PutWithSealedOrderHandling(Shift shift);

    Task CopyRequiredQualificationsAsync(Guid sourceShiftId, Guid targetShiftId, CancellationToken cancellationToken = default);

    Task<SporadicShiftInfo?> GetSporadicInfoAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plannable shifts of the real plan (OriginalShift or SplitShift, no scenario copy) whose name or abbreviation equals
    /// the given text, ignoring case. Not filtered by group visibility: the caller must filter before it reveals anything.
    /// </summary>
    /// <param name="nameOrAbbreviation">Shift name or abbreviation as the user typed it</param>
    /// <param name="limit">Maximum number of shifts returned</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<Shift>> FindPlannableByNameAsync(string nameOrAbbreviation, int limit, CancellationToken cancellationToken = default);

    Task<bool> HasActiveWorksAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<bool> HasWorksForGroupAsync(Guid groupId, DateOnly? afterDate = null, CancellationToken cancellationToken = default);

    Task<bool> HasWorksForAnyGroupAsync(IEnumerable<Guid> groupIds, DateOnly? afterDate = null, CancellationToken cancellationToken = default);

    Task<bool> HasWorksForClientInGroupAsync(Guid clientId, Guid groupId, DateOnly? afterDate = null, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetShiftIdsByClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<bool> HasWorksAfterDateForOrderTreeAsync(Guid sealedOrderId, DateOnly afterDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// What the family of a sealed order (the order, its plannable copy and every cut piece, found by OriginalId, scenario
    /// clones excluded) is still used by: works of any kind that are not deleted, scenario clones of its shifts and
    /// container references. Read-only.
    /// </summary>
    /// <param name="sealedOrderId">Id of the sealed order</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<OrderFamilyUsage> GetOrderFamilyUsageAsync(Guid sealedOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the soft delete of the given shifts together with their group links, default expenses and required
    /// qualifications. Stage-only like Delete: the caller commits through the unit of work.
    /// </summary>
    /// <param name="shiftIds">Ids of the shifts to delete, as returned by GetOrderFamilyUsageAsync</param>
    /// <param name="cancellationToken">Cancels the query that loads the shifts</param>
    Task DeleteOrderFamilyAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);
}
