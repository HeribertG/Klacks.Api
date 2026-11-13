// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Application.Interfaces
{
    public interface IGetAllClientIdsFromGroupAndSubgroups
    {
        /// <summary>
        /// Every client with a group item in the group or one of its subgroups, whatever the item is: regular
        /// memberships, temporary memberships of borrowed days and memberships scenarios hold. For readers that ask
        /// whom a change of the plan concerns (notifications, recalculation, the people a source scenario covers), not
        /// who the group's own people are; for those use GetRealClientIdsFromGroupAndSubgroups.
        /// </summary>
        /// <param name="groupId">Group whose subtree is read; an unknown group throws KeyNotFoundException</param>
        Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId);

        /// <summary>
        /// The clients with a regular membership (RegularMembership) in the group or one of its subgroups. A
        /// temporary membership does not count, neither one of a scenario nor one a taken-over cover left in the real
        /// plan: a person borrowed for single days stays a borrowed person.
        /// </summary>
        /// <param name="groupId">Group whose subtree is read</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task<List<Guid>> GetRealClientIdsFromGroupAndSubgroups(Guid groupId, CancellationToken cancellationToken);

        Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds);

        Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroups(Guid groupId);

        Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroupsFromList(List<Guid> groupIds);
    }
}
