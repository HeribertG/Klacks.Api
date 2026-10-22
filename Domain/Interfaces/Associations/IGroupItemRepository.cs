// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IGroupItemRepository : IBaseRepository<GroupItem>
{
    /// <summary>
    /// Returns the real (non-scenario) membership of a client in a group, or <c>null</c> when there is
    /// none. Scenario memberships (AnalyseToken set) are never returned: they may coexist with the real
    /// one for the same client and group, and callers of this method always read or change the real
    /// membership. The filtered unique index on (client, group) for real, non-deleted rows guarantees
    /// that at most one row can match.
    /// </summary>
    Task<GroupItem?> GetByClientAndGroup(Guid clientId, Guid groupId);

    Task<int> CountExistingByIds(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the pull-forward of the start of many memberships in a few queries (ids in chunks): each listed item whose
    /// ValidFrom lies after the given start gets that start (also when a retried call finds the entity already carrying it but
    /// still unsaved, so the count stays the number of rows that change); an item that is already on or before it, has no start, or is
    /// deleted is left alone, so the call never moves a start later. The caller commits via IUnitOfWork. Returns the number
    /// of items changed.
    /// </summary>
    /// <param name="newStarts">New ValidFrom per group item id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> PullForwardValidFromAsync(IReadOnlyDictionary<Guid, DateTime> newStarts, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the ended real memberships (ValidUntil set, no scenario, no shift item, not deleted) of many clients in a few
    /// queries (ids in chunks), so a pull-forward can keep a start behind the end of an earlier group membership.
    /// </summary>
    /// <param name="clientIds">Clients whose ended memberships are read</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<(Guid ClientId, Guid GroupId, DateTime ValidUntil)>> GetEndedMembershipsAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the current ValidFrom of many items in a few queries (ids in chunks), without tracking, so a caller can
    /// verify a committed change. Items that do not exist or are deleted are missing from the result.
    /// </summary>
    /// <param name="ids">Group item ids</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Dictionary<Guid, DateTime?>> GetValidFromByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages the (soft) deletion of every listed link in one query; the caller commits via IUnitOfWork.
    /// Returns the number of links found and staged.
    /// </summary>
    Task<int> RemoveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Each shift linked to one of the groups once, as id, name and abbreviation, ordered by name. A link counts when it
    /// is not deleted (scenario links included, validity not checked); deleted shifts are left out.
    /// </summary>
    /// <param name="groupIds">Groups whose shift links are read, matched exactly (not by subtree)</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<LinkedShiftName>> GetLinkedShiftNamesAsync(
        IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Per group, the number of its real client rows: items with a client, without a scenario token and not deleted;
    /// their validity is not checked, so ended memberships count too. Groups without such a row are missing.
    /// </summary>
    /// <param name="groupIds">Groups whose client rows are counted</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<Dictionary<Guid, int>> CountRealClientRowsPerGroupAsync(
        IReadOnlyCollection<Guid> groupIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Id of a link between the shift and the group that is not deleted, or null when there is none. Neither ordered nor
    /// restricted to the real plan: when a scenario link exists next to the real one, either id may come back.
    /// </summary>
    /// <param name="shiftId">Linked shift</param>
    /// <param name="groupId">Group the shift is linked to</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<Guid?> FindShiftLinkIdAsync(Guid shiftId, Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Per client, the period the schedule grid derives from its items in the given groups: the earliest ValidFrom and
    /// the latest ValidUntil, where an item without a bound leaves that side open (GroupItemValidityRange). Only clients with at least one
    /// non-deleted item in those groups are present; the group ids are matched exactly, not by subtree.
    /// </summary>
    /// <param name="clientIds">Clients whose group items are read</param>
    /// <param name="groupIds">Groups the items must belong to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<Dictionary<Guid, (DateTime? ValidFrom, DateTime? ValidUntil)>> GetValidityRangesAsync(
        IReadOnlyCollection<Guid> clientIds,
        IReadOnlyCollection<Guid> groupIds,
        CancellationToken cancellationToken = default);

    Task<List<Guid>> GetGroupIdsByShiftId(Guid shiftId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetShiftIdsByGroupIds(List<Guid> groupIds, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetShiftCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetCustomerCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetEmployeeCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetExternEmpCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<List<Guid>> GetGroupTreeIdsForClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads the group facts the visibility rule decides a client by, with the same predicates as
    /// ClientGroupFilterService; null when the client does not exist or is soft-deleted.
    /// </summary>
    Task<ClientVisibilityMembership?> GetVisibilityMembershipAsync(Guid clientId, CancellationToken cancellationToken = default);
}
