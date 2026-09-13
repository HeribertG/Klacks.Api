// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, read-only planner for partition_clients_by_address: turns a client list and the currently
/// existing groups into the region/state/city (or cluster) hierarchy the skill would create or reuse,
/// plus the per-client leaf placement. It never touches the database — the command handler is the only
/// place that writes. The "current address" of a client is resolved the same way
/// <see cref="CustomerGroupingPlanner"/> does. Region parents come from the country's region map in the
/// context (a country without a map gets no region level) unless a caller-supplied root group overrides
/// it, in which case every state (or, at City level, every city) attaches directly under that root.
/// At Cluster level the leaf nodes are the density clusters computed by <see cref="AddressClusterPlanner"/>.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupPartitionPlanner
{
    private const string ReasonNoAddress = "no address on record";
    private const string ReasonNoState = "address has no state/province";
    private const string ReasonNoCity = "address has no city";
    private const string ReasonNoStateAndCity = "address has neither state/province nor city";

    private const string RegionKeyPrefix = "region:";
    private const string StateKeyPrefix = "state:";
    private const string CityKeyPrefix = "city:";
    private const string ClusterKeyPrefix = "cluster:";
    private const string NameParentKeySeparator = "|group-key|";
    private const string RootParentMarker = "root";

    private sealed record PlacedClient(Client Client, string Country, string State, string City, double? Latitude, double? Longitude);

    private sealed class PlanBuilder
    {
        public PlanBuilder(List<UnassignablePartitionClient> unassignable)
        {
            Unassignable = unassignable;
        }

        public List<PlannedPartitionGroup> Groups { get; } = new();

        public Dictionary<string, Guid?> ResolvedId { get; } = new(StringComparer.Ordinal);

        public Dictionary<string, string> StateKeyByCountryAndCode { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<Guid, string> LeafKeyByClientId { get; } = new();

        public List<UnassignablePartitionClient> Unassignable { get; }
    }

    /// <summary>
    /// Plans the group tree and the leaf placement for the given clients.
    /// </summary>
    /// <param name="clients">Clients of every requested entity type with addresses and memberships loaded</param>
    /// <param name="existingGroups">All groups currently in the database</param>
    /// <param name="level">Granularity of the tree</param>
    /// <param name="rootGroupId">Optional root every top-level node attaches under; null uses the region map</param>
    /// <param name="includeAlreadyGrouped">When false, clients with an active membership are skipped</param>
    /// <param name="context">Default country, region maps, state names and the cluster share</param>
    public static GroupPartitionPlan Plan(
        IReadOnlyList<Client> clients,
        IReadOnlyList<Group> existingGroups,
        GroupPartitionLevelEnum level,
        Guid? rootGroupId,
        bool includeAlreadyGrouped,
        GroupPartitionContext context)
    {
        var activeGroups = existingGroups
            .Where(g => !g.IsDeleted && !string.IsNullOrWhiteSpace(g.Name))
            .ToList();
        var existingByNameAndParent = activeGroups
            .GroupBy(g => NameParentKey(g.Name, g.Parent))
            .Where(byKey => byKey.Count() == 1)
            .ToDictionary(byKey => byKey.Key, byKey => byKey.First());
        var groupsByNameAnywhere = activeGroups.ToLookup(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase);

        var skipped = 0;
        var unassignable = new List<UnassignablePartitionClient>();
        var placed = new List<PlacedClient>();

        foreach (var client in clients)
        {
            if (!includeAlreadyGrouped && HasActiveMembership(client))
            {
                skipped++;
                continue;
            }

            var address = CustomerGroupingPlanner.SelectPreferredAddress(client, _ => true);
            var country = string.IsNullOrWhiteSpace(address?.Country)
                ? context.DefaultCountryCode.Trim().ToUpperInvariant()
                : address!.Country.Trim().ToUpperInvariant();
            var state = address?.State?.Trim().ToUpperInvariant() ?? string.Empty;
            var city = address?.City?.Trim() ?? string.Empty;

            var reason = ResolveUnassignableReason(level, address, state, city);
            if (reason != null)
            {
                unassignable.Add(new UnassignablePartitionClient(client.Id, DisplayName(client), reason));
                continue;
            }

            placed.Add(new PlacedClient(client, country, state, city, address?.Latitude, address?.Longitude));
        }

        var builder = new PlanBuilder(unassignable);

        if (level != GroupPartitionLevelEnum.City)
        {
            PlanRegionsAndStates(level, rootGroupId, context, placed, existingByNameAndParent, builder);
        }

        switch (level)
        {
            case GroupPartitionLevelEnum.State:
                foreach (var client in placed)
                {
                    builder.LeafKeyByClientId[client.Client.Id] = builder.StateKeyByCountryAndCode[StateLookupKey(client.Country, client.State)];
                }

                break;
            case GroupPartitionLevelEnum.City:
                PlanFlatCities(rootGroupId, placed, existingByNameAndParent, builder);
                break;
            case GroupPartitionLevelEnum.StateCity:
                PlanCitiesUnderStates(placed, existingByNameAndParent, builder);
                break;
            case GroupPartitionLevelEnum.Cluster:
                PlanClustersUnderStates(context.ClusterSharePercent, placed, existingByNameAndParent, builder);
                break;
        }

        var assignments = placed
            .Where(c => builder.LeafKeyByClientId.ContainsKey(c.Client.Id))
            .Select(c => new PartitionClientAssignment(c.Client.Id, DisplayName(c.Client), builder.LeafKeyByClientId[c.Client.Id]))
            .ToList();

        var warnings = BuildDuplicateNameWarnings(builder.Groups, groupsByNameAnywhere);

        return new GroupPartitionPlan(clients.Count, skipped, builder.Groups, assignments, unassignable, warnings);
    }

    private static void PlanRegionsAndStates(
        GroupPartitionLevelEnum level,
        Guid? rootGroupId,
        GroupPartitionContext context,
        List<PlacedClient> placed,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var neededStates = placed
            .Select(c => (c.Country, c.State))
            .Distinct()
            .OrderBy(s => s.Country, StringComparer.Ordinal)
            .ThenBy(s => s.State, StringComparer.Ordinal)
            .ToList();

        var regionKeyByCountryAndName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (rootGroupId is null)
        {
            var regions = neededStates
                .Select(s => (s.Country, Region: RegionFor(context, s.Country, s.State)))
                .Where(r => r.Region != null)
                .Select(r => (r.Country, Region: r.Region!))
                .Distinct()
                .OrderBy(r => r.Country, StringComparer.Ordinal)
                .ThenBy(r => r.Region, StringComparer.Ordinal);

            foreach (var (country, regionName) in regions)
            {
                var key = RegionKeyPrefix + country + GroupPartitionContext.StateKeySeparator + regionName;
                var existing = LookupExisting(existingByNameAndParent, regionName, parentActualId: null, parentIsPending: false);
                builder.Groups.Add(new PlannedPartitionGroup(key, regionName, ParentKey: null, existing != null, existing?.Id, ClientCount: 0));
                regionKeyByCountryAndName[country + GroupPartitionContext.StateKeySeparator + regionName] = key;
                builder.ResolvedId[key] = existing?.Id;
            }
        }

        foreach (var (country, stateCode) in neededStates)
        {
            string? parentKey = null;
            Guid? parentActualId = rootGroupId;
            var parentIsPending = false;

            var regionName = rootGroupId is null ? RegionFor(context, country, stateCode) : null;
            if (regionName != null)
            {
                parentKey = regionKeyByCountryAndName[country + GroupPartitionContext.StateKeySeparator + regionName];
                parentActualId = builder.ResolvedId[parentKey];
                parentIsPending = parentActualId is null;
            }

            var key = StateKeyPrefix + country + GroupPartitionContext.StateKeySeparator + stateCode;
            var existing = LookupExisting(existingByNameAndParent, stateCode, parentActualId, parentIsPending);
            var clientCount = level == GroupPartitionLevelEnum.State
                ? placed.Count(c => c.Country == country && string.Equals(c.State, stateCode, StringComparison.OrdinalIgnoreCase))
                : 0;
            var description = context.StateNameByCountryAndCode.TryGetValue(GroupPartitionContext.StateKey(country, stateCode), out var name)
                ? name
                : stateCode;

            builder.Groups.Add(new PlannedPartitionGroup(key, stateCode, parentKey, existing != null, existing?.Id, clientCount, description));
            builder.StateKeyByCountryAndCode[StateLookupKey(country, stateCode)] = key;
            builder.ResolvedId[key] = existing?.Id;
        }
    }

    private static void PlanFlatCities(
        Guid? rootGroupId,
        List<PlacedClient> placed,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var cityKeyByCity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var neededCities = placed
            .Select(c => c.City)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.Ordinal);

        foreach (var city in neededCities)
        {
            var key = CityKeyPrefix + city;
            var existing = LookupExisting(existingByNameAndParent, city, rootGroupId, parentIsPending: false);
            var clientCount = placed.Count(c => string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));

            builder.Groups.Add(new PlannedPartitionGroup(key, city, ParentKey: null, existing != null, existing?.Id, clientCount));
            cityKeyByCity[city] = key;
        }

        foreach (var client in placed)
        {
            builder.LeafKeyByClientId[client.Client.Id] = cityKeyByCity[client.City];
        }
    }

    private static void PlanCitiesUnderStates(
        List<PlacedClient> placed,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var cityKeyByStateAndCity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var neededCities = placed
            .GroupBy(c => (c.Country, c.State, CityUpper: c.City.ToUpperInvariant()))
            .Select(g => (g.Key.Country, g.Key.State, City: g.First().City))
            .OrderBy(c => c.Country, StringComparer.Ordinal)
            .ThenBy(c => c.State, StringComparer.Ordinal)
            .ThenBy(c => c.City, StringComparer.Ordinal);

        foreach (var (country, state, city) in neededCities)
        {
            var stateKey = builder.StateKeyByCountryAndCode[StateLookupKey(country, state)];
            var parentActualId = builder.ResolvedId[stateKey];
            var parentIsPending = parentActualId is null;

            var key = CityKeyPrefix + country + GroupPartitionContext.StateKeySeparator + state + GroupPartitionContext.StateKeySeparator + city;
            var existing = LookupExisting(existingByNameAndParent, city, parentActualId, parentIsPending);
            var clientCount = placed.Count(c =>
                c.Country == country &&
                string.Equals(c.State, state, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));

            builder.Groups.Add(new PlannedPartitionGroup(key, city, stateKey, existing != null, existing?.Id, clientCount));
            cityKeyByStateAndCity[StateLookupKey(country, state) + GroupPartitionContext.StateKeySeparator + city] = key;
        }

        foreach (var client in placed)
        {
            builder.LeafKeyByClientId[client.Client.Id] =
                cityKeyByStateAndCity[StateLookupKey(client.Country, client.State) + GroupPartitionContext.StateKeySeparator + client.City];
        }
    }

    private static void PlanClustersUnderStates(
        int sharePercent,
        List<PlacedClient> placed,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var clientById = placed.ToDictionary(c => c.Client.Id, c => c.Client);
        var clusterPlan = AddressClusterPlanner.Plan(
            placed.Select(c => new ClusterAddress(c.Client.Id, c.Country, c.State, c.City, c.Latitude, c.Longitude)).ToList(),
            sharePercent);

        var clusterKeyByCluster = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cluster in clusterPlan.Clusters)
        {
            var stateKey = builder.StateKeyByCountryAndCode[StateLookupKey(cluster.Country, cluster.State)];
            var parentActualId = builder.ResolvedId[stateKey];
            var parentIsPending = parentActualId is null;

            var key = ClusterKeyPrefix + cluster.Country + GroupPartitionContext.StateKeySeparator + cluster.State + GroupPartitionContext.StateKeySeparator + cluster.City;
            var existing = LookupExisting(existingByNameAndParent, cluster.City, parentActualId, parentIsPending);

            builder.Groups.Add(new PlannedPartitionGroup(
                key, cluster.City, stateKey, existing != null, existing?.Id,
                cluster.DirectCount + cluster.AttachedCount, string.Empty, cluster.Latitude, cluster.Longitude));
            clusterKeyByCluster[ClusterLookupKey(cluster.Country, cluster.State, cluster.City)] = key;
        }

        foreach (var assignment in clusterPlan.Assignments)
        {
            builder.LeafKeyByClientId[assignment.ClientId] = clusterKeyByCluster[ClusterLookupKey(assignment.Country, assignment.State, assignment.City)];
        }

        foreach (var rejection in clusterPlan.Rejections)
        {
            builder.Unassignable.Add(new UnassignablePartitionClient(rejection.ClientId, DisplayName(clientById[rejection.ClientId]), rejection.Reason));
        }
    }

    private static string? RegionFor(GroupPartitionContext context, string country, string stateCode) =>
        context.RegionByCountryAndState.TryGetValue(country, out var byState) && byState.TryGetValue(stateCode, out var region)
            ? region
            : null;

    private static string StateLookupKey(string country, string state) => country + GroupPartitionContext.StateKeySeparator + state;

    private static string ClusterLookupKey(string country, string state, string city) =>
        country + GroupPartitionContext.StateKeySeparator + state + GroupPartitionContext.StateKeySeparator + city;

    private static List<string> BuildDuplicateNameWarnings(
        IReadOnlyList<PlannedPartitionGroup> groups, ILookup<string, Group> groupsByNameAnywhere)
    {
        var warnings = new List<string>();
        var warnedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var planned in groups.Where(g => !g.Existed))
        {
            if (!groupsByNameAnywhere.Contains(planned.Name) || !warnedNames.Add(planned.Name))
            {
                continue;
            }

            var elsewhereIds = string.Join(", ", groupsByNameAnywhere[planned.Name].Select(g => g.Id));
            warnings.Add(
                $"A group named '{planned.Name}' already exists elsewhere in the tree (id {elsewhereIds}); " +
                $"creating another '{planned.Name}' will make name-based group matching " +
                "(e.g. group_ungrouped_by_city_name, propose_grouping) ambiguous for this name everywhere.");
        }

        return warnings;
    }

    private static Group? LookupExisting(
        Dictionary<string, Group> existingByNameAndParent,
        string name,
        Guid? parentActualId,
        bool parentIsPending)
    {
        if (parentIsPending)
        {
            return null;
        }

        return existingByNameAndParent.TryGetValue(NameParentKey(name, parentActualId), out var found) ? found : null;
    }

    private static string NameParentKey(string name, Guid? parentId) =>
        name.Trim().ToUpperInvariant() + NameParentKeySeparator + (parentId?.ToString() ?? RootParentMarker);

    private static bool HasActiveMembership(Client client) =>
        client.GroupItems.Any(gi => !gi.IsDeleted && gi.AnalyseToken == null);

    private static string? ResolveUnassignableReason(
        GroupPartitionLevelEnum level, Address? address, string state, string city)
    {
        if (address == null)
        {
            return ReasonNoAddress;
        }

        return level switch
        {
            GroupPartitionLevelEnum.State => string.IsNullOrEmpty(state) ? ReasonNoState : null,
            GroupPartitionLevelEnum.City => string.IsNullOrEmpty(city) ? ReasonNoCity : null,
            _ => string.IsNullOrEmpty(state) && string.IsNullOrEmpty(city)
                ? ReasonNoStateAndCity
                : string.IsNullOrEmpty(state)
                    ? ReasonNoState
                    : string.IsNullOrEmpty(city)
                        ? ReasonNoCity
                        : null
        };
    }

    private static string DisplayName(Client client)
    {
        if (!string.IsNullOrWhiteSpace(client.Company))
        {
            return client.Company!;
        }

        var name = $"{client.FirstName} {client.Name}".Trim();
        return string.IsNullOrWhiteSpace(name) ? client.Name : name;
    }
}
