// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies the order location filter to a shift query in the database: the order's location address
/// (OrderLocationAddressRule.LocatedAt) must match the criteria, and for a restricted caller the customer must also be
/// visible to them (ClientGroupVisibilityFilter), so a location search never reveals where a hidden customer lives.
/// A null or empty filter leaves the query unchanged.
/// </summary>
/// <param name="query">Shift query the filter is added to</param>
/// <param name="clients">The client set the customer visibility is checked against</param>
/// <param name="location">Resolved location filter; null = no location filter</param>

using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftLocationQuery
{
    public static IQueryable<Shift> Apply(IQueryable<Shift> query, IQueryable<Client> clients, ShiftLocationFilter? location)
    {
        if (location == null || location.Criteria.IsEmpty)
        {
            return query;
        }

        query = query.Where(OrderLocationAddressRule.LocatedAt(location.Criteria));

        if (location.CustomerVisibleGroupIds == null)
        {
            return query;
        }

        var visibleCustomerIds = clients
            .Where(ClientGroupVisibilityFilter.VisibleTo(location.CustomerVisibleGroupIds))
            .Select(c => c.Id);

        return query.Where(s => s.ClientId != null && visibleCustomerIds.Contains(s.ClientId.Value));
    }
}
