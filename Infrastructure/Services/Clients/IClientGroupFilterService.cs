// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Infrastructure.Services.Clients;

/// <summary>
/// Narrows a client query to what the selected group and the caller's group visibility allow (see
/// ClientGroupFilterService), for the Infrastructure reads that compose on it; Application code only asks
/// ICallerGroupRestriction.
/// </summary>
public interface IClientGroupFilterService : ICallerGroupRestriction
{
    Task<IQueryable<Client>> FilterClientsByGroupId(
        Guid? selectedGroupId, IQueryable<Client> query, bool withoutGroup = false);

    /// <summary>
    /// Narrows the query to the clients with a regular membership (RegularMembership) in the selected group or one of
    /// its subgroups, intersected with the groups the caller may see like FilterClientsByGroupId. A person who was only
    /// borrowed into the group for single days is left out; FilterClientsByGroupId keeps listing her.
    /// </summary>
    /// <param name="selectedGroupId">Selected group; may be a root, an inner node or a leaf</param>
    /// <param name="query">Client query to narrow</param>
    Task<IQueryable<Client>> FilterRegularMembersOfGroup(Guid selectedGroupId, IQueryable<Client> query);
}
