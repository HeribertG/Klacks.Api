// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    IQueryable<GroupItem> GetQuery();

    Task<List<Guid>> GetGroupIdsByShiftId(Guid shiftId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetShiftIdsByGroupIds(List<Guid> groupIds, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetShiftCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetCustomerCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetEmployeeCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetExternEmpCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<List<Guid>> GetGroupTreeIdsForClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}
