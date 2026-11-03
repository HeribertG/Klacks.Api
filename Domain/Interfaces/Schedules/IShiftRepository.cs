// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Geo;
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

    /// <summary>
    /// Whether a shift with status OriginalShift already carries <paramref name="originalShiftId"/> as its OriginalId
    /// inside the world <paramref name="analyseToken"/> (null = the real plan, a token = the variant that token belongs to).
    /// </summary>
    /// <param name="originalShiftId">Id of the sealed order</param>
    /// <param name="analyseToken">Analyse token the copy must carry; null for the real plan</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<bool> HasOriginalShiftCopyAsync(Guid originalShiftId, Guid? analyseToken = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the given shifts untracked, with their client and the client's addresses.
    /// </summary>
    Task<List<Shift>> GetWithClientByIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The filtered, paginated shift/order list. The location filter (optional) narrows the list to orders whose
    /// location (OrderLocationAddressRule) matches; it is ANDed with every other filter and with the group visibility.
    /// </summary>
    /// <param name="filter">List view, date range, text search, sorting and paging</param>
    /// <param name="visibleGroupIds">The caller's group restriction (null = unrestricted)</param>
    /// <param name="location">Resolved order location filter with the caller's customer visibility; null = none</param>
    Task<TruncatedShift> GetFilteredAndPaginatedShifts(
        ShiftFilter filter, IReadOnlyCollection<Guid>? visibleGroupIds, ShiftLocationFilter? location = null);

    Task UpdateGroupItems(Guid shiftId, List<Guid> actualGroupIds);
    
    Task<List<Group>> GetGroupsForShift(Guid shiftId);

    /// <summary>
    /// The live rows of one order (OriginalId = the sealed order) inside one world, ordered by tree position. A row without
    /// an order (a container) has no rows pointing at it, so its list is empty.
    /// </summary>
    /// <param name="id">Id of the sealed order, or of one of its plannable or cut rows</param>
    /// <param name="filterClosedBefore">Leaves out rows that ended before this date</param>
    /// <param name="tracked">Whether the rows are change-tracked</param>
    /// <param name="analyseToken">Analyse token the rows must carry; null for the real plan</param>
    Task<List<Shift>> CutList(Guid id, DateOnly? filterClosedBefore = null, bool tracked = false, Guid? analyseToken = null);

    /// <summary>
    /// The sealed order with its group items, inside one world.
    /// </summary>
    /// <param name="originalId">Id of the sealed order</param>
    /// <param name="analyseToken">Analyse token the sealed order must carry; null for the real plan</param>
    Task<Shift?> GetSealedOrder(Guid originalId, Guid? analyseToken = null);

    /// <summary>
    /// Adds a shift. A sealed order gets its plannable copy, which is returned. A sealed container gets no copy: it is
    /// stored as the one row of ContainerShiftRules and that row is returned.
    /// </summary>
    /// <param name="shift">The shift to add</param>
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

    /// <summary>
    /// Saves a shift. The save that seals an order creates its plannable copy, which is returned. The save that seals a
    /// container creates no copy: the row takes the shape of ContainerShiftRules and is returned itself.
    /// </summary>
    /// <param name="shift">The shift to save; a form save cannot change its cut hierarchy</param>
    Task<Shift?> PutWithSealedOrderHandling(Shift shift);

    Task CopyRequiredQualificationsAsync(Guid sourceShiftId, Guid targetShiftId, CancellationToken cancellationToken = default);

    Task<SporadicShiftInfo?> GetSporadicInfoAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plannable shifts of the real plan (OriginalShift or SplitShift, no scenario copy) whose name or abbreviation equals
    /// the given text, ignoring case, that the caller may see. The group-visibility rule (ShiftGroupVisibilityFilter) is
    /// applied in the query BEFORE the limit, so hidden same-named shifts can never use up the limit and push a visible
    /// one out.
    /// </summary>
    /// <param name="nameOrAbbreviation">Shift name or abbreviation as the user typed it</param>
    /// <param name="visibleGroupIds">The caller's restriction from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync
    /// (null = unrestricted, empty = only group-less shifts); deliberately not optional, like for GetFilteredAndPaginatedShifts</param>
    /// <param name="limit">Maximum number of shifts returned</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<Shift>> FindPlannableByNameAsync(
        string nameOrAbbreviation,
        IReadOnlyCollection<Guid>? visibleGroupIds,
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a real-plan work (not deleted, no analyse token) sits on the shift. Works never carry the token of a
    /// variant (key), so the answer for a key shift is false.
    /// </summary>
    /// <param name="shiftId">Id of the shift</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<bool> HasActiveWorksAsync(Guid shiftId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Whether a not deleted shift has this shift as its parent. Reads the rows, not the nested-set values: a tree whose
    /// numbering failed after a key was promoted has rows without Lft and Rgt.
    /// </summary>
    /// <param name="shiftId">Id of the shift</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<bool> HasChildrenAsync(Guid shiftId, CancellationToken cancellationToken = default);

    Task<bool> HasWorksForGroupAsync(Guid groupId, DateOnly? afterDate = null, CancellationToken cancellationToken = default);

    Task<bool> HasWorksForAnyGroupAsync(IEnumerable<Guid> groupIds, DateOnly? afterDate = null, CancellationToken cancellationToken = default);

    Task<bool> HasWorksForClientInGroupAsync(Guid clientId, Guid groupId, DateOnly? afterDate = null, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetShiftIdsByClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// What the family of a sealed order (the order, its plannable copy and every cut piece, found by OriginalId, scenario
    /// clones excluded) is still used by: works of any kind that are not deleted, scenario clones of its shifts and
    /// container references. Read-only. Only rows without an analyse token are the family, so for a sealed order of a
    /// variant (key) the family is empty; key families are deleted through IVariantShiftWriteGuard.ResolveDeletableFamilyAsync.
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
