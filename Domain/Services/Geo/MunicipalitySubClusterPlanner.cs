// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure density clustering one level below a city cluster. A place other than the cluster's centre city
/// becomes a sub-cluster when it holds at least the share threshold of the cluster's addresses AND at least
/// <see cref="MinimumSubClusterMembers"/> addresses. Every other non-centre place is attached as a whole to the
/// nearest sub-cluster when that sub-cluster's centre is strictly nearer to the place's mean position than the
/// city cluster's centre; otherwise (no sub-cluster, missing coordinates, or nearer to the centre) it stays a
/// direct member of the city cluster. Centre-city addresses always stay in the city cluster, so no sub-cluster
/// ever carries the name of its parent. Place names are compared case-insensitively. Deterministic:
/// sub-clusters and attachments are ordered by name (ordinal).
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Services.Geo;

public static class MunicipalitySubClusterPlanner
{
    public const int MinimumSubClusterMembers = 3;

    private const int MinSharePercent = 1;
    private const int MaxSharePercent = 100;
    private const int PercentBase = 100;
    private const string ShareOutOfRangeMessageTemplate = "Share must be between {0} and {1}.";

    /// <param name="centreCity">Name of the city cluster's centre city</param>
    /// <param name="centreLatitude">Latitude of the city cluster's centre; null when unknown</param>
    /// <param name="centreLongitude">Longitude of the city cluster's centre; null when unknown</param>
    /// <param name="members">Every address placed into the city cluster, each with its own city</param>
    /// <param name="sharePercent">Minimum share (1-100) of the cluster's addresses a place needs to become a sub-cluster</param>
    public static MunicipalitySubClusterPlan Plan(
        string centreCity,
        double? centreLatitude,
        double? centreLongitude,
        IReadOnlyList<ClusterAddress> members,
        int sharePercent)
    {
        ArgumentNullException.ThrowIfNull(members);

        if (sharePercent < MinSharePercent || sharePercent > MaxSharePercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sharePercent),
                sharePercent,
                string.Format(CultureInfo.InvariantCulture, ShareOutOfRangeMessageTemplate, MinSharePercent, MaxSharePercent));
        }

        var total = members.Count;
        var places = members
            .Where(m => !string.Equals(m.City.Trim(), centreCity.Trim(), StringComparison.OrdinalIgnoreCase))
            .GroupBy(m => m.City.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => BuildPlace(g.ToList()))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        var centres = places
            .Where(p => p.Addresses.Count >= MinimumSubClusterMembers
                && p.Addresses.Count * PercentBase >= sharePercent * total)
            .ToList();
        var centreNames = new HashSet<string>(centres.Select(c => c.Name), StringComparer.Ordinal);

        var subClusterByClientId = new Dictionary<Guid, string>();
        var attachedCount = centres.ToDictionary(c => c.Name, _ => 0, StringComparer.Ordinal);
        foreach (var centre in centres)
        {
            foreach (var address in centre.Addresses)
            {
                subClusterByClientId[address.ClientId] = centre.Name;
            }
        }

        var anchors = centres
            .Where(c => c.Latitude.HasValue && c.Longitude.HasValue)
            .ToList();

        var attachments = new List<MunicipalityPlaceAttachment>();
        foreach (var place in places.Where(p => !centreNames.Contains(p.Name)))
        {
            var target = NearestCloserThanCentre(place, anchors, centreLatitude, centreLongitude);
            if (target is null)
            {
                attachments.Add(new MunicipalityPlaceAttachment(place.Name, null, place.Addresses.Count, null));
                continue;
            }

            attachedCount[target.Value.Name] += place.Addresses.Count;
            foreach (var address in place.Addresses)
            {
                subClusterByClientId[address.ClientId] = target.Value.Name;
            }

            attachments.Add(new MunicipalityPlaceAttachment(
                place.Name, target.Value.Name, place.Addresses.Count, target.Value.DistanceKm));
        }

        var subClusters = centres
            .Select(c => new MunicipalitySubCluster(c.Name, c.Latitude, c.Longitude, c.Addresses.Count, attachedCount[c.Name]))
            .ToList();

        return new MunicipalitySubClusterPlan(subClusters, subClusterByClientId, attachments);
    }

    private static (string Name, double DistanceKm)? NearestCloserThanCentre(
        Place place, List<Place> anchors, double? centreLatitude, double? centreLongitude)
    {
        if (anchors.Count == 0 || !place.Latitude.HasValue || !place.Longitude.HasValue
            || !centreLatitude.HasValue || !centreLongitude.HasValue)
        {
            return null;
        }

        var best = anchors[0];
        var bestDistance = double.MaxValue;
        foreach (var anchor in anchors)
        {
            var distance = HaversineDistanceCalculator.DistanceKm(
                place.Latitude.Value, place.Longitude.Value, anchor.Latitude!.Value, anchor.Longitude!.Value);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = anchor;
            }
        }

        var centreDistance = HaversineDistanceCalculator.DistanceKm(
            place.Latitude.Value, place.Longitude.Value, centreLatitude.Value, centreLongitude.Value);

        return bestDistance < centreDistance ? (best.Name, bestDistance) : null;
    }

    private static Place BuildPlace(List<ClusterAddress> addresses)
    {
        var name = addresses
            .GroupBy(a => a.City.Trim(), StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .First().Key;

        var located = addresses.Where(a => a.Latitude.HasValue && a.Longitude.HasValue).ToList();
        return located.Count == 0
            ? new Place(name, addresses, null, null)
            : new Place(name, addresses, located.Average(a => a.Latitude!.Value), located.Average(a => a.Longitude!.Value));
    }

    private sealed record Place(string Name, List<ClusterAddress> Addresses, double? Latitude, double? Longitude);
}
