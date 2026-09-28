// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿namespace Klacks.Api.Application.Interfaces
{
    public interface IGetAllClientIdsFromGroupAndSubgroups
    {
        Task<List<Guid>> GetAllClientIdsFromGroupAndSubgroups(Guid groupId);

        Task<List<Guid>> GetAllClientIdsFromGroupsAndSubgroupsFromList(List<Guid> groupIds);

        Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroups(Guid groupId);

        Task<HashSet<Guid>> GetAllGroupIdsIncludingSubgroupsFromList(List<Guid> groupIds);
    }
}
