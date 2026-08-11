// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupRepository : IBaseRepository<Group>
{
    new Task<Group?> Get(Guid id);

    Task<bool> SetCoordinatesAsync(Guid groupId, double latitude, double longitude, CancellationToken cancellationToken = default);

    Task<bool> MarkGeocodingAttemptedAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetUnattemptedGeocodingCandidateIdsAsync(CancellationToken cancellationToken = default);

    Task<GroupGeocodingStatus> GetGeocodingStatusAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetGroupIdsWithMembersAsync(CancellationToken cancellationToken = default);

    Task<TruncatedGroup> Truncated(GroupFilter filter);

    Task MoveNode(Guid nodeId, Guid newParentId);

    Task<IEnumerable<Group>> GetChildren(Guid parentId);

    Task<IEnumerable<Group>> GetTree(Guid? rootId = null);

    Task<IEnumerable<Group>> GetPath(Guid nodeId);

    Task<int> GetNodeDepth(Guid nodeId);

    Task RepairNestedSetValues();

    Task FixRootValues();

    Task<IEnumerable<Group>> GetRoots();
}
