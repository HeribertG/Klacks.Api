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
    /// Stages the (soft) deletion of every listed link in one query; the caller commits via IUnitOfWork.
    /// Returns the number of links found and staged.
    /// </summary>
    Task<int> RemoveByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default);

    IQueryable<GroupItem> GetQuery();

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
