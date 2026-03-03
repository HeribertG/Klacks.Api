// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupItemRepository : IBaseRepository<GroupItem>
{
    Task<GroupItem?> GetByClientAndGroup(Guid clientId, Guid groupId);

    IQueryable<GroupItem> GetQuery();

    Task<List<Guid>> GetGroupIdsByShiftId(Guid shiftId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetShiftIdsByGroupIds(List<Guid> groupIds, CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetShiftCountsPerGroupAsync(CancellationToken cancellationToken = default);

    Task<Dictionary<Guid, int>> GetCustomerCountsPerGroupAsync(CancellationToken cancellationToken = default);
}
