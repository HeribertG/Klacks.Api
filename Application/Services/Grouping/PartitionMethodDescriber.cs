// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Describes in one English sentence how a partition_clients_by_address run decides group membership, so the
/// assistant can repeat the method to the user instead of guessing it (e.g. inventing that every address needs
/// coordinates). At cluster levels the sentence names the share threshold and the centre threshold per state.
/// </summary>
/// <param name="level">Granularity of the run</param>
/// <param name="clusterSharePercent">Minimum share of a state's addresses a city needs to become a cluster centre</param>
/// <param name="subClusterSharePercent">Minimum share of a city cluster's addresses a place needs to become a municipality sub-cluster</param>
/// <param name="thresholds">Population and centre threshold per state, as computed by the cluster planner</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Grouping;

public static class PartitionMethodDescriber
{
    private const int MaxListedThresholds = 10;
    private const string ThresholdSeparator = ", ";
    private const string CountryStateSeparator = "/";

    private const string StateMethod =
        "Method: every client joins the group of the state/province code of its address; no coordinates are used.";

    private const string CityMethod =
        "Method: every client joins the group named after the city of its address; no coordinates are used.";

    private const string StateCityMethod =
        "Method: every client joins the group of the city of its address, below the group of its state/province; " +
        "no coordinates are used.";

    private const string ClusterMethodTemplate =
        "Method: cluster centres are the cities holding at least {0}% of their state's addresses " +
        "(the largest city of a state always is one{1}); clients of a centre city join it by city name; every other " +
        "place joins the nearest centre of its state as a whole, measured from the mean position of its geocoded " +
        "addresses (a state with a single centre takes every place without measuring); with several centres, a place " +
        "without any geocoded address cannot be placed. Addresses themselves need no coordinates.";

    private const string ThresholdsTemplate = "; thresholds: {0}";
    private const string ThresholdTemplate = "{0} {1} of {2}";
    private const string MoreThresholdsTemplate = " (+{0} more)";

    private const string MunicipalityMethodTemplate =
        " Inside every city cluster, a place other than the centre city becomes a municipality sub-cluster when it " +
        "holds at least {0}% of the cluster's addresses and at least {1}; smaller places join the nearest sub-cluster " +
        "as a whole when it is nearer than the cluster centre, otherwise they stay directly in the city cluster.";

    public static string Describe(
        GroupPartitionLevelEnum level,
        int clusterSharePercent,
        int subClusterSharePercent,
        IReadOnlyList<ClusterStateThreshold> thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);

        return level switch
        {
            GroupPartitionLevelEnum.State => StateMethod,
            GroupPartitionLevelEnum.City => CityMethod,
            GroupPartitionLevelEnum.StateCity => StateCityMethod,
            GroupPartitionLevelEnum.Cluster => DescribeCluster(clusterSharePercent, thresholds),
            GroupPartitionLevelEnum.ClusterMunicipality =>
                DescribeCluster(clusterSharePercent, thresholds) +
                string.Format(
                    CultureInfo.InvariantCulture,
                    MunicipalityMethodTemplate,
                    subClusterSharePercent,
                    MunicipalitySubClusterPlanner.MinimumSubClusterMembers),
            _ => string.Empty
        };
    }

    private static string DescribeCluster(int sharePercent, IReadOnlyList<ClusterStateThreshold> thresholds) =>
        string.Format(CultureInfo.InvariantCulture, ClusterMethodTemplate, sharePercent, DescribeThresholds(thresholds));

    private static string DescribeThresholds(IReadOnlyList<ClusterStateThreshold> thresholds)
    {
        if (thresholds.Count == 0)
        {
            return string.Empty;
        }

        var severalCountries = thresholds.Select(t => t.Country).Distinct(StringComparer.Ordinal).Count() > 1;
        var listed = thresholds
            .OrderByDescending(t => t.Population)
            .ThenBy(t => t.Country, StringComparer.Ordinal)
            .ThenBy(t => t.State, StringComparer.Ordinal)
            .Take(MaxListedThresholds)
            .Select(t => string.Format(
                CultureInfo.InvariantCulture,
                ThresholdTemplate,
                severalCountries ? t.Country + CountryStateSeparator + t.State : t.State,
                t.MinimumForCentre,
                t.Population));
        var more = thresholds.Count > MaxListedThresholds
            ? string.Format(CultureInfo.InvariantCulture, MoreThresholdsTemplate, thresholds.Count - MaxListedThresholds)
            : string.Empty;

        return string.Format(CultureInfo.InvariantCulture, ThresholdsTemplate, string.Join(ThresholdSeparator, listed) + more);
    }
}
