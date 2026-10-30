// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure, deterministic geographic clustering of located items into a fixed number of clusters. The points are put in the
/// order of their ids first, so the result does not depend on the order of the input. The centers are seeded by farthest
/// point (the point farthest from the overall centroid, then repeatedly the point farthest from all chosen centers), then
/// k-means runs on great-circle distances with the centers as the mean latitude and longitude of their members, until no
/// point changes its cluster or MaxIterations is reached. Ties go to the lower id or lower cluster index, a cluster that
/// loses all its points is refilled with the point farthest from its own center in a cluster of several points, so no
/// cluster is ever empty, and the clusters come back ordered from west to east. No randomness. The mean of the
/// coordinates is meant for the extent of one region; it is not defined across the antimeridian.
/// </summary>

using System.Globalization;

namespace Klacks.Api.Domain.Services.Geo;

public static class GeographicClusterer
{
    public const int MaxIterations = 50;

    private const int MinClusterCount = 1;
    private const int MinMembersToDonate = 2;
    private const string ClusterCountTooSmallMessage = "clusterCount must be at least 1.";
    private const string TooFewPointsMessage = "clusterCount ({0}) must not exceed the number of points ({1}).";
    private const string DuplicateIdMessage = "The ids of the points must be unique.";

    private readonly record struct Center(double Latitude, double Longitude);

    /// <param name="points">The located items; ids must be unique and there must be at least clusterCount of them</param>
    /// <param name="clusterCount">Number of clusters, at least 1</param>
    public static IReadOnlyList<GeographicCluster> Cluster(IReadOnlyList<GeographicClusterPoint> points, int clusterCount)
    {
        ArgumentNullException.ThrowIfNull(points);

        if (clusterCount < MinClusterCount)
        {
            throw new ArgumentOutOfRangeException(nameof(clusterCount), clusterCount, ClusterCountTooSmallMessage);
        }

        if (clusterCount > points.Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(clusterCount),
                clusterCount,
                string.Format(CultureInfo.InvariantCulture, TooFewPointsMessage, clusterCount, points.Count));
        }

        var ordered = points.OrderBy(point => point.Id).ToArray();
        if (ordered.Select(point => point.Id).Distinct().Count() != ordered.Length)
        {
            throw new ArgumentException(DuplicateIdMessage, nameof(points));
        }

        var centers = SeedCenters(ordered, clusterCount);
        var assignment = Enumerable.Repeat(-1, ordered.Length).ToArray();

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            var changed = AssignToNearestCenter(ordered, centers, assignment);
            changed |= RefillEmptyClusters(ordered, centers, assignment);
            if (!changed)
            {
                break;
            }

            RecomputeCenters(ordered, assignment, centers);
        }

        RecomputeCenters(ordered, assignment, centers);
        return BuildClusters(ordered, assignment, centers);
    }

    private static Center[] SeedCenters(GeographicClusterPoint[] ordered, int clusterCount)
    {
        var overall = new Center(ordered.Average(p => p.Latitude), ordered.Average(p => p.Longitude));
        var chosen = new List<int> { IndexOfFarthest(ordered, chosen: [], [overall]) };

        while (chosen.Count < clusterCount)
        {
            chosen.Add(IndexOfFarthest(ordered, chosen, chosen.Select(i => PointCenter(ordered[i])).ToList()));
        }

        return chosen.Select(i => PointCenter(ordered[i])).ToArray();
    }

    private static int IndexOfFarthest(GeographicClusterPoint[] ordered, List<int> chosen, List<Center> from)
    {
        var best = -1;
        var bestDistance = double.NegativeInfinity;

        for (var i = 0; i < ordered.Length; i++)
        {
            if (chosen.Contains(i))
            {
                continue;
            }

            var distance = from.Min(center => DistanceKm(ordered[i], center));
            if (distance > bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static bool AssignToNearestCenter(GeographicClusterPoint[] ordered, Center[] centers, int[] assignment)
    {
        var changed = false;
        for (var i = 0; i < ordered.Length; i++)
        {
            var nearest = NearestCenter(ordered[i], centers);
            if (assignment[i] != nearest)
            {
                assignment[i] = nearest;
                changed = true;
            }
        }

        return changed;
    }

    private static int NearestCenter(GeographicClusterPoint point, Center[] centers)
    {
        var nearest = 0;
        var nearestDistance = double.PositiveInfinity;

        for (var c = 0; c < centers.Length; c++)
        {
            var distance = DistanceKm(point, centers[c]);
            if (distance < nearestDistance)
            {
                nearest = c;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private static bool RefillEmptyClusters(GeographicClusterPoint[] ordered, Center[] centers, int[] assignment)
    {
        var changed = false;
        var sizes = new int[centers.Length];
        foreach (var cluster in assignment)
        {
            sizes[cluster]++;
        }

        for (var c = 0; c < centers.Length; c++)
        {
            if (sizes[c] > 0)
            {
                continue;
            }

            var donor = IndexOfFarthestFromOwnCenter(ordered, centers, assignment, sizes);
            sizes[assignment[donor]]--;
            assignment[donor] = c;
            sizes[c]++;
            centers[c] = PointCenter(ordered[donor]);
            changed = true;
        }

        return changed;
    }

    private static int IndexOfFarthestFromOwnCenter(
        GeographicClusterPoint[] ordered, Center[] centers, int[] assignment, int[] sizes)
    {
        var best = -1;
        var bestDistance = double.NegativeInfinity;

        for (var i = 0; i < ordered.Length; i++)
        {
            if (sizes[assignment[i]] < MinMembersToDonate)
            {
                continue;
            }

            var distance = DistanceKm(ordered[i], centers[assignment[i]]);
            if (distance > bestDistance)
            {
                best = i;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static void RecomputeCenters(GeographicClusterPoint[] ordered, int[] assignment, Center[] centers)
    {
        for (var c = 0; c < centers.Length; c++)
        {
            var members = Enumerable.Range(0, ordered.Length).Where(i => assignment[i] == c).Select(i => ordered[i]).ToList();
            if (members.Count > 0)
            {
                centers[c] = new Center(members.Average(p => p.Latitude), members.Average(p => p.Longitude));
            }
        }
    }

    private static List<GeographicCluster> BuildClusters(GeographicClusterPoint[] ordered, int[] assignment, Center[] centers)
    {
        var unindexed = Enumerable.Range(0, centers.Length)
            .Select(c => (
                Members: Enumerable.Range(0, ordered.Length).Where(i => assignment[i] == c).Select(i => ordered[i].Id).ToList(),
                Center: centers[c]))
            .ToList();

        return unindexed
            .OrderBy(cluster => cluster.Center.Longitude)
            .ThenBy(cluster => cluster.Center.Latitude)
            .ThenBy(cluster => cluster.Members[0])
            .Select((cluster, index) => new GeographicCluster(
                index, cluster.Members, cluster.Center.Latitude, cluster.Center.Longitude))
            .ToList();
    }

    private static Center PointCenter(GeographicClusterPoint point) => new(point.Latitude, point.Longitude);

    private static double DistanceKm(GeographicClusterPoint point, Center center) =>
        HaversineDistanceCalculator.DistanceKm(point.Latitude, point.Longitude, center.Latitude, center.Longitude);
}
