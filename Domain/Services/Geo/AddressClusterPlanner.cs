// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure density clustering of addresses per (country, state): a city whose share of the state's
/// addresses reaches the threshold becomes a cluster center, and the largest city of every state is a
/// center regardless. Addresses of a center city join it by city name. Every other place joins the
/// nearest center of its own state AS A WHOLE, measured by great-circle distance from the place
/// position (the mean of its geocoded addresses, see <see cref="PlacePosition"/>) to the center's
/// coordinates (the mean of the center city's geocoded addresses), so neighbours of one village never
/// split. Single addresses need no coordinates. When a state has exactly one center, every other place
/// of the state joins it even without a measurable distance (DistanceKm null) since it is the only place
/// to go. With several centers, a place without any geocoded address is rejected as a whole
/// (<see cref="ReasonPlaceWithoutPosition"/>), and a place is rejected when no center has coordinates
/// (<see cref="ReasonNoCenterCoordinates"/>). Deterministic: clusters are ordered by country, state and
/// city, place attachments by country, state and place (ordinal).
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Services.Geo;

public static class AddressClusterPlanner
{
    public const string ReasonPlaceWithoutPosition =
        "place '{0}' has no geocoded address, so its distance to the cluster centres cannot be measured";

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

        return new AddressClusterPlan(
            statePlans.SelectMany(p => p.Clusters).ToList(),
            statePlans.SelectMany(p => p.Assignments).ToList(),
            statePlans.SelectMany(p => p.Rejections).ToList(),
            statePlans.SelectMany(p => p.PlaceAttachments).ToList(),
            statePlans.Select(p => p.Threshold).ToList());
    }

    /// <summary>
    /// Smallest number of addresses a city needs to become a center of a state.
    /// </summary>
    /// <param name="population">Number of addresses of the state</param>
    /// <param name="sharePercent">Minimum share (1-100) of the state's addresses</param>
    public static int MinimumForCentre(int population, int sharePercent) =>
        (int)Math.Ceiling(sharePercent * (double)population / PercentBase);

    private static StatePlan PlanState(string country, string state, List<ClusterAddress> stateAddresses, int sharePercent)
    {
        var byCity = stateAddresses
            .GroupBy(a => a.City, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Place(CanonicalCityName(g), g.ToList()))
            .OrderByDescending(c => c.Addresses.Count)
            .ThenBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        var centers = DetermineCenters(byCity, sharePercent, stateAddresses.Count);
        var centerNames = new HashSet<string>(centers.Select(c => c.Name), StringComparer.Ordinal);

        var direct = AssignDirect(country, state, centers);
        var attached = AttachRemaining(country, state, byCity, centerNames, direct.Anchors, direct.AttachedCount);

        var assignments = direct.Assignments.Concat(attached.Assignments).ToList();
        var clusters = BuildClusters(country, state, centers, direct.Positions, direct.AttachedCount);
        var threshold = new ClusterStateThreshold(
            country, state, stateAddresses.Count, MinimumForCentre(stateAddresses.Count, sharePercent));

        return new StatePlan(clusters, assignments, attached.Rejections, attached.PlaceAttachments, threshold);
    }

    private static List<Place> DetermineCenters(List<Place> byCity, int sharePercent, int total) =>
        byCity
            .Where((c, index) => index == 0 || (c.Addresses.Count * PercentBase) >= (sharePercent * total))
            .ToList();

    private static (List<AddressClusterAssignment> Assignments, List<CenterAnchor> Anchors, Dictionary<string, PlacePosition?> Positions, Dictionary<string, int> AttachedCount) AssignDirect(
        string country, string state, List<Place> centers)
    {
        var assignments = new List<AddressClusterAssignment>();
        var anchors = new List<CenterAnchor>();
        var positions = new Dictionary<string, PlacePosition?>(StringComparer.Ordinal);
        var attachedCount = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var center in centers)
        {
            var position = PlacePosition.Of(center.Addresses);
            positions[center.Name] = position;
            attachedCount[center.Name] = 0;
            if (position.HasValue)
            {
                anchors.Add(new CenterAnchor(center.Name, position.Value.Latitude, position.Value.Longitude));
            }

            foreach (var address in center.Addresses)
            {
                assignments.Add(new AddressClusterAssignment(address.ClientId, country, state, center.Name, AttachedByDistance: false, DistanceKm: null));
            }
        }

        return (assignments, anchors, positions, attachedCount);
    }

    private static (List<AddressClusterAssignment> Assignments, List<AddressClusterRejection> Rejections, List<ClusterPlaceAttachment> PlaceAttachments) AttachRemaining(
        string country,
        string state,
        List<Place> byCity,
        HashSet<string> centerNames,
        List<CenterAnchor> anchors,
        Dictionary<string, int> attachedCount)
    {
        var assignments = new List<AddressClusterAssignment>();
        var rejections = new List<AddressClusterRejection>();
        var placeAttachments = new List<ClusterPlaceAttachment>();

        foreach (var place in byCity.Where(c => !centerNames.Contains(c.Name)).OrderBy(c => c.Name, StringComparer.Ordinal))
        {
            var position = PlacePosition.Of(place.Addresses);
            var target = ResolveTarget(place, position, centerNames, anchors);
            if (target.RejectionReason != null)
            {
                rejections.AddRange(place.Addresses.Select(a => new AddressClusterRejection(a.ClientId, target.RejectionReason)));
                continue;
            }

            var centre = target.Centre!;
            attachedCount[centre] += place.Addresses.Count;
            assignments.AddRange(place.Addresses.Select(a =>
                new AddressClusterAssignment(a.ClientId, country, state, centre, AttachedByDistance: true, target.DistanceKm)));
            placeAttachments.Add(new ClusterPlaceAttachment(
                country, state, place.Name, centre, place.Addresses.Count, position?.LocatedAddressCount ?? 0, target.DistanceKm));
        }

        return (assignments, rejections, placeAttachments);
    }

    private static (string? Centre, double? DistanceKm, string? RejectionReason) ResolveTarget(
        Place place, PlacePosition? position, HashSet<string> centerNames, List<CenterAnchor> anchors)
    {
        if (position.HasValue && anchors.Count > 0)
        {
            var (nearest, distance) = Nearest(position.Value.Latitude, position.Value.Longitude, anchors);
            return (nearest, distance, null);
        }

        if (centerNames.Count == 1)
        {
            return (centerNames.Single(), null, null);
        }

        return position.HasValue
            ? (null, null, ReasonNoCenterCoordinates)
            : (null, null, string.Format(CultureInfo.InvariantCulture, ReasonPlaceWithoutPosition, place.Name));
    }

    private static List<AddressCluster> BuildClusters(
        string country,
        string state,
        List<Place> centers,
        Dictionary<string, PlacePosition?> positions,
        Dictionary<string, int> attachedCount)
    {
        var clusters = new List<AddressCluster>();
        foreach (var center in centers.OrderBy(c => c.Name, StringComparer.Ordinal))
        {
            var position = positions[center.Name];
            clusters.Add(new AddressCluster(
                country, state, center.Name, position?.Latitude, position?.Longitude, center.Addresses.Count, attachedCount[center.Name]));
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

    private sealed record Place(string Name, List<ClusterAddress> Addresses);

    private sealed record StatePlan(
        IReadOnlyList<AddressCluster> Clusters,
        IReadOnlyList<AddressClusterAssignment> Assignments,
        IReadOnlyList<AddressClusterRejection> Rejections,
        IReadOnlyList<ClusterPlaceAttachment> PlaceAttachments,
        ClusterStateThreshold Threshold);
}
