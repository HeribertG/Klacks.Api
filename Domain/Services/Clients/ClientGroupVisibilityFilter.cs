// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The client group-visibility rule of the client list for a restricted caller, as an expression: a client is visible
/// when it has no group membership at all or a real (non-scenario) membership in one of the visible groups. Applied by
/// ClientGroupFilterService and by the order location filter, which must not find orders by the address of a customer
/// the caller may not see.
/// </summary>
/// <param name="visibleGroupIds">Groups the caller may see (flat, subgroups included)</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Clients;

public static class ClientGroupVisibilityFilter
{
    public static Expression<Func<Client, bool>> VisibleTo(IReadOnlyCollection<Guid> visibleGroupIds)
    {
        var allowedGroupIds = visibleGroupIds.ToList();
        return client => !client.GroupItems.Any()
                         || client.GroupItems.Any(gi => allowedGroupIds.Contains(gi.GroupId) && gi.AnalyseToken == null);
    }
}
