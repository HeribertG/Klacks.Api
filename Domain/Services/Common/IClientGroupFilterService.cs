// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public interface IClientGroupFilterService
{
    Task<IQueryable<Client>> FilterClientsByGroupId(
        Guid? selectedGroupId, IQueryable<Client> query, bool withoutGroup = false);

    /// <summary>
    /// True when group visibility does not limit the caller at all: an admin, an installation without groups,
    /// or work without a calling user (neither an HTTP user nor an execution principal).
    /// </summary>
    Task<bool> IsCallerUnrestrictedAsync();
}