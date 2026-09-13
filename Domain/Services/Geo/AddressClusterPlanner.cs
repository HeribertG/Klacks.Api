// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure density clustering of addresses per (country, state): a city whose share of the state's
/// addresses reaches the threshold becomes a cluster center, the largest city of every state is a
/// center regardless, and every other address is attached to the nearest center of its own state by
/// great-circle distance. Center coordinates are the mean of the center city's address coordinates.
/// When a state has exactly one center and that center has no coordinates, every remaining address of
/// the state is still attached to it (AttachedByDistance true, DistanceKm null) since it is the only
/// place to go; with several coordinate-less centers the remaining addresses are rejected instead
/// because which one is nearest cannot be determined. Deterministic: clusters are ordered by country,
/// state and city (ordinal).
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Services.Geo;

public static class AddressClusterPlanner
{
    public const string ReasonNoCoordinates = "address lies outside every cluster city and has no coordinates";
    public const string ReasonNoCenterCoordinates = "no cluster city of this state has coordinates to measure the distance to";

    private const int MinSharePercent = 1;
    private const int MaxSharePercent = 100;
    private const int PercentBase = 100;
    private const string ShareOutOfRangeMessageTemplate = "Share must be between {0} and {1}.";

    /// <param name="addresses">One entry per client with the address fields the caller resolved</param>
    /// <param name="sharePercent">Minimum share (1-100) of a state's addresses a city needs to be a center</param>
    public static AddressClusterPlan Plan(IReadOnlyList<ClusterAddress> addresses, int sharePercent)
    {
        ArgumentNullException.ThrowIfNull(addresses);

        if (sharePercent < MinSharePercent || sharePercent > MaxSharePercent)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sharePercent),
                sharePercent,
                string.Format(CultureInfo.InvariantCulture, ShareOutOfRangeMessageTemplate, MinSharePercent, MaxSharePercent));
        }

        var byState = addresses
            .Select(a => a with { Country = a.Country.Trim().ToUpperInvariant(), State = a.State.Trim().ToUpperInvariant(), City = a.City.Trim() })
            .GroupBy(a => (a.Country, a.State))
            .OrderBy(g => g.Key.Country, StringComparer.Ordinal)
            .ThenBy(g => g.Key.State, StringComparer.Ordinal);

        var statePlans = byState
            .Select(g => PlanState(g.Key.Country, g.Key.State, g.ToList(), sharePercent))
            .ToList();

        var clusters = statePlans.SelectMany(p => p.Clusters).ToList();
        var assignments = statePlans.SelectMany(p => p.Assignments).ToList();
        var rejections = statePlans.SelectMany(p => p.Rejections).ToList();

        return new AddressClusterPlan(clusters, assignments, rejections);
    }

    private static StatePlan PlanState(string country, string state, List<ClusterAddress> stateAddresses, int sharePercent)
    {
        var byCity = stateAddresses
            .GroupBy(a => a.City, StringComparer.OrdinalIgnoreCase)
            .Select(g => (Name: CanonicalCityName(g), Addresses: g.ToList()))
            .OrderByDescending(c => c.Addresses.Count)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        var centers = DetermineCenters(byCity, sharePercent, stateAddresses.Count);
        var centerNames = new HashSet<string>(centers.Select(c => c.Name), StringComparer.Ordinal);

        var direct = AssignDirect(country, state, centers);
        var attached = AttachRemaining(country, state, byCity, centerNames, direct.Anchors, direct.AttachedCount);

        var assignments = direct.Assignments.Concat(attached.Assignments).ToList();
        var clusters = BuildClusters(country, state, centers, direct.Centroids, direct.AttachedCount);

        return new StatePlan(clusters, assignments, attached.Rejections);
    }

    private static List<(string Name, List<ClusterAddress> Addresses)> DetermineCenters(
        List<(string Name, List<ClusterAddress> Addresses)> byCity, int sharePercent, int total) =>
        byCity
            .Where((c, index) => index == 0 || (c.Addresses.Count * PercentBase) >= (sharePercent * total))
            .ToList();

    private static (List<AddressClusterAssignment> Assignments, List<CenterAnchor> Anchors, Dictionary<string, Centroid> Centroids, Dictionary<string, int> AttachedCount) AssignDirect(
        string country, string state, List<(string Name, List<ClusterAddress> Addresses)> centers)
    {
        var assignments = new List<AddressClusterAssignment>();
        var anchors = new List<CenterAnchor>();
        var centroids = new Dictionary<string, Centroid>(StringComparer.Ordinal);
        var attachedCount = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var center in centers)
        {
            var centroid = ComputeCentroid(center.Addresses);
            centroids[center.Name] = centroid;
            attachedCount[center.Name] = 0;
            if (centroid.Latitude.HasValue && centroid.Longitude.HasValue)
            {
                anchors.Add(new CenterAnchor(center.Name, centroid.Latitude.Value, centroid.Longitude.Value));
            }

            foreach (var address in center.Addresses)
            {
                assignments.Add(new AddressClusterAssignment(address.ClientId, country, state, center.Name, AttachedByDistance: false, DistanceKm: null));
            }
        }

        return (assignments, anchors, centroids, attachedCount);
    }

    private static (List<AddressClusterAssignment> Assignments, List<AddressClusterRejection> Rejections) AttachRemaining(
        string country,
        string state,
        List<(string Name, List<ClusterAddress> Addresses)> byCity,
        HashSet<string> centerNames,
        List<CenterAnchor> anchors,
        Dictionary<string, int> attachedCount)
    {
        var assignments = new List<AddressClusterAssignment>();
        var rejections = new List<AddressClusterRejection>();

        foreach (var town in byCity.Where(c => !centerNames.Contains(c.Name)))
        {
            foreach (var address in town.Addresses)
            {
                if (!address.Latitude.HasValue || !address.Longitude.HasValue)
                {
                    rejections.Add(new AddressClusterRejection(address.ClientId, ReasonNoCoordinates));
                    continue;
                }

                if (anchors.Count == 0)
                {
                    if (centerNames.Count == 1)
                    {
                        var onlyCenter = centerNames.Single();
                        attachedCount[onlyCenter]++;
                        assignments.Add(new AddressClusterAssignment(address.ClientId, country, state, onlyCenter, AttachedByDistance: true, DistanceKm: null));
                        continue;
                    }

                    rejections.Add(new AddressClusterRejection(address.ClientId, ReasonNoCenterCoordinates));
                    continue;
                }

                var (nearest, distance) = Nearest(address.Latitude.Value, address.Longitude.Value, anchors);
                attachedCount[nearest]++;
                assignments.Add(new AddressClusterAssignment(address.ClientId, country, state, nearest, AttachedByDistance: true, distance));
            }
        }

        return (assignments, rejections);
    }

    private static List<AddressCluster> BuildClusters(
        string country,
        string state,
        List<(string Name, List<ClusterAddress> Addresses)> centers,
        Dictionary<string, Centroid> centroids,
        Dictionary<string, int> attachedCount)
    {
        var clusters = new List<AddressCluster>();
        foreach (var center in centers.OrderBy(c => c.Name, StringComparer.Ordinal))
        {
            var centroid = centroids[center.Name];
            clusters.Add(new AddressCluster(country, state, center.Name, centroid.Latitude, centroid.Longitude, center.Addresses.Count, attachedCount[center.Name]));
        }

        return clusters;
    }

    private static string CanonicalCityName(IGrouping<string, ClusterAddress> cityGroup) =>
        cityGroup
            .GroupBy(a => a.City, StringComparer.Ordinal)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => IsAllUpper(g.Key))
            .ThenBy(g => g.Key, StringComparer.Ordinal)
            .First().Key;

    private static bool IsAllUpper(string value) =>
        value.Length > 0 && value.All(c => !char.IsLower(c));

    private static Centroid ComputeCentroid(List<ClusterAddress> addresses)
    {
        var located = addresses.Where(a => a.Latitude.HasValue && a.Longitude.HasValue).ToList();
        if (located.Count == 0)
        {
            return new Centroid(null, null);
        }

        return new Centroid(located.Average(a => a.Latitude!.Value), located.Average(a => a.Longitude!.Value));
    }

    private static (string City, double DistanceKm) Nearest(double latitude, double longitude, List<CenterAnchor> anchors)
    {
        var best = anchors[0];
        var bestDistance = double.MaxValue;
        foreach (var anchor in anchors)
        {
            var distance = HaversineDistanceCalculator.DistanceKm(latitude, longitude, anchor.Latitude, anchor.Longitude);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = anchor;
            }
        }

        return (best.City, bestDistance);
    }

    private readonly record struct CenterAnchor(string City, double Latitude, double Longitude);

    private readonly record struct Centroid(double? Latitude, double? Longitude);

    private sealed record StatePlan(
        IReadOnlyList<AddressCluster> Clusters,
        IReadOnlyList<AddressClusterAssignment> Assignments,
        IReadOnlyList<AddressClusterRejection> Rejections);
}
