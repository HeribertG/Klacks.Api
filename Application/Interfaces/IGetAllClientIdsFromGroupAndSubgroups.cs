// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Application.Interfaces
{
    public interface IGetAllClientIdsFromGroupAndSubgroups
    {
        Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId);

        /// <summary>
        /// The clients with a membership of the real plan in the group or one of its subgroups. A temporary membership
        /// of a scenario does not count.
        /// </summary>
        /// <param name="groupId">Group whose subtree is read</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<List<Guid>> GetRealClientIdsFromGroupAndSubgroups(Guid groupId, CancellationToken cancellationToken);

        Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds);

        Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroups(Guid groupId);

        Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroupsFromList(List<Guid> groupIds);
    }
}
