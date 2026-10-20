// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupRepository : IBaseRepository<Group>
{
    new Task<Group?> Get(Guid id);

    /// <summary>
    /// Lowers the ValidFrom of each listed group to the given start when the group currently starts later; a group that
    /// starts on or before it is left alone, so a start is never moved later. Runs immediately as one update per group inside
    /// the ambient transaction (it does not go through the unit of work). Returns the number of groups changed.
    /// </summary>
    /// <param name="newStarts">Lowest member start per group id</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<int> LowerValidFromAsync(IReadOnlyDictionary<Guid, DateTime> newStarts, CancellationToken cancellationToken = default);

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
