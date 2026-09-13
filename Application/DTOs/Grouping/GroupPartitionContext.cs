// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Reference data GroupPartitionPlanner needs beyond the clients and groups, resolved by the handler so
/// the planner stays a pure function. Keys are looked up with trimmed, upper-cased country and state
/// codes; build the maps with <see cref="StringComparer.OrdinalIgnoreCase"/> or upper-cased keys.
/// </summary>
/// <param name="DefaultCountryCode">Country assumed for addresses that carry no country.</param>
/// <param name="RegionByCountryAndState">Per country code, the state-code-to-region-name map; a country without an entry (or an empty map) gets no region level.</param>
/// <param name="StateNameByCountryAndCode">Display name per "COUNTRY|STATE" key (upper-case), used as the description of a state node; a missing key falls back to the code.</param>
/// <param name="ClusterSharePercent">Minimum share (1-100) of a state's addresses a city needs to become a cluster center.</param>
public sealed record GroupPartitionContext(
    string DefaultCountryCode,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> RegionByCountryAndState,
    IReadOnlyDictionary<string, string> StateNameByCountryAndCode,
    int ClusterSharePercent)
{
    public const string StateKeySeparator = "|";

    public const int DefaultClusterSharePercent = 10;

    public static string StateKey(string countryCode, string stateCode) =>
        countryCode.Trim().ToUpperInvariant() + StateKeySeparator + stateCode.Trim().ToUpperInvariant();
}
