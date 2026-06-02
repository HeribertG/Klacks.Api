// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure nearest-anchor assignment: maps a customer location to the geographically closest group
/// anchor (great-circle distance). Anchor-agnostic — the caller decides which groups are anchors
/// (e.g. groups carrying coordinates) and which customers participate.
/// </summary>

namespace Klacks.Api.Domain.Services.Geo;

public static class CustomerGroupAssigner
{
    /// <summary>
    /// Returns the nearest anchor to the customer, or <c>null</c> when there are no anchors.
    /// </summary>
    /// <param name="customer">The customer location to assign.</param>
    /// <param name="anchors">Candidate group anchors (groups carrying coordinates).</param>
    public static CustomerGroupAssignment? FindNearest(CustomerLocation customer, IReadOnlyList<GroupAnchor> anchors)
    {
        if (anchors.Count == 0)
        {
            return null;
        }

        GroupAnchor? best = null;
        var bestDistance = double.MaxValue;

        foreach (var anchor in anchors)
        {
            var distance = HaversineDistanceCalculator.DistanceKm(
                customer.Latitude, customer.Longitude, anchor.Latitude, anchor.Longitude);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = anchor;
            }
        }

        return new CustomerGroupAssignment(customer.ClientId, best!.GroupId, bestDistance);
    }
}
