// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
﻿using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IGroupVisibilityRepository : IBaseRepository<GroupVisibility>
{
    Task<IEnumerable<GroupVisibility>> GroupVisibilityList(string id);

    Task<IEnumerable<GroupVisibility>> GetGroupVisibilityList();

    Task SetGroupVisibilityList(List<GroupVisibility> list);

    /// <summary>
    /// Counts the non-admin users holding an explicit visibility entry on the given group (read-only).
    /// </summary>
    /// <param name="groupId">The group whose explicit visibility entries are counted</param>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<int> CountNonAdminUsersSeeingGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
}
