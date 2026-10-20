// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure, read-only planner for partition_clients_by_address: turns a client list and the currently
/// existing groups into the region/state/city (or cluster) hierarchy the skill would create or reuse,
/// plus the per-client leaf placement. It never touches the database — the command handler is the only
/// place that writes. The "current address" of a client is resolved the same way
/// <see cref="CustomerGroupingPlanner"/> does. Region parents come from the country's region map in the
/// context (a country without a map gets no region level) unless a caller-supplied root group overrides
/// it, in which case every state (or, at City level, every city) attaches directly under that root.
/// At Cluster level the leaf nodes are the density clusters computed by <see cref="AddressClusterPlanner"/>.
/// At ClusterMunicipality level every city cluster additionally gets the municipality sub-clusters computed by
/// <see cref="MunicipalitySubClusterPlanner"/>; clients of the centre city and of small places that stay with
/// the centre remain direct members of the city cluster group.
/// Population versus candidates: the cluster structure (centres, thresholds, centroids, sub-clusters) is
/// always computed from EVERY placeable client of the states that have candidates, also those already
/// grouped, while includeAlreadyGrouped=false only decides who gets a NEW membership (the candidates). A
/// re-run after a partial first run therefore reproduces the same structure and reuses its groups by name
/// and parent instead of turning leftover villages into centres of their own. Nodes without candidates are
/// only listed when they already exist.
/// Regions are identified by the language-independent key of the country's region file; the display name
/// only labels a newly created root. An existing root group is reused when it carries ANY language variant
/// of the region name, so switching the installation language never creates a second root. A state that
/// already exists under the root of ANOTHER region of the same country is not moved; the plan warns instead.
/// Country codes are normalised to ISO alpha-2 (the seeded US states carry "USA"), so "US" and "USA"
/// addresses share one state node and one region file.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
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
    private const string MunicipalityKeyPrefix = "municipality:";
    private const string NameParentKeySeparator = "|group-key|";
    private const string RootParentMarker = "root";

    private const string AmbiguousRegionRootWarningTemplate =
        "Several root groups carry a name of region '{0}' ({1}); none was reused and a new root '{0}' would be created. " +
        "Merge or rename the duplicate roots first.";

    private const string StateUnderOtherRegionWarningTemplate =
        "{0} exists under '{1}'; the run creates a second '{0}' under '{2}' — move the existing group first.";

    private sealed record PlacedClient(Client Client, string Country, string State, string City, double? Latitude, double? Longitude, bool IsCandidate);

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

        public List<PartitionPlaceAttachment> PlaceAttachments { get; } = new();

        public List<ClusterPlaceAttachment> ClusterPlaceAttachments { get; } = new();

        public List<ClusterStateThreshold> StateThresholds { get; } = new();

        public List<string> Warnings { get; } = new();

        public HashSet<string> NamesWithSpecificWarning { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Plans the group tree and the leaf placement for the given clients.
    /// </summary>
    /// <param name="clients">Clients of every requested entity type with addresses and memberships loaded</param>
    /// <param name="existingGroups">All groups currently in the database</param>
    /// <param name="level">Granularity of the tree</param>
    /// <param name="rootGroupId">Optional root every top-level node attaches under; null uses the region map</param>
    /// <param name="includeAlreadyGrouped">When false, clients with an active membership get no new membership; they still count for the cluster structure</param>
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
            var isCandidate = includeAlreadyGrouped || !HasActiveMembership(client);
            if (!isCandidate)
            {
                skipped++;
            }

            var address = CustomerGroupingPlanner.SelectPreferredAddress(client, _ => true);
            var country = NormalizeCountry(string.IsNullOrWhiteSpace(address?.Country)
                ? context.DefaultCountryCode
                : address!.Country);
            var state = address?.State?.Trim().ToUpperInvariant() ?? string.Empty;
            var city = address?.City?.Trim() ?? string.Empty;

            var reason = ResolveUnassignableReason(level, address, state, city);
            if (reason != null)
            {
                if (isCandidate)
                {
                    unassignable.Add(new UnassignablePartitionClient(client.Id, DisplayName(client), reason));
                }

                continue;
            }

            placed.Add(new PlacedClient(client, country, state, city, address?.Latitude, address?.Longitude, isCandidate));
        }

        var candidates = placed.Where(c => c.IsCandidate).ToList();
        var builder = new PlanBuilder(unassignable);

        if (level != GroupPartitionLevelEnum.City)
        {
            PlanRegionsAndStates(level, rootGroupId, context, candidates, activeGroups, existingByNameAndParent, builder);
        }

        switch (level)
        {
            case GroupPartitionLevelEnum.State:
                foreach (var client in candidates)
                {
                    builder.LeafKeyByClientId[client.Client.Id] = builder.StateKeyByCountryAndCode[StateLookupKey(client.Country, client.State)];
                }

                break;
            case GroupPartitionLevelEnum.City:
                PlanFlatCities(rootGroupId, candidates, existingByNameAndParent, builder);
                break;
            case GroupPartitionLevelEnum.StateCity:
                PlanCitiesUnderStates(candidates, existingByNameAndParent, builder);
                break;
            case GroupPartitionLevelEnum.Cluster:
                PlanClustersUnderStates(context.ClusterSharePercent, placed, existingByNameAndParent, builder);
                break;
            case GroupPartitionLevelEnum.ClusterMunicipality:
                var clusterPlan = PlanClustersUnderStates(context.ClusterSharePercent, placed, existingByNameAndParent, builder);
                PlanMunicipalitiesUnderClusters(context.SubClusterSharePercent, clusterPlan, placed, existingByNameAndParent, builder);
                break;
        }

        var assignments = candidates
            .Where(c => builder.LeafKeyByClientId.ContainsKey(c.Client.Id))
            .Select(c => new PartitionClientAssignment(c.Client.Id, DisplayName(c.Client), builder.LeafKeyByClientId[c.Client.Id]))
            .ToList();

        var warnings = builder.Warnings
            .Concat(BuildDuplicateNameWarnings(builder.Groups, groupsByNameAnywhere, builder.NamesWithSpecificWarning))
            .ToList();

        return new GroupPartitionPlan(
            clients.Count, skipped, builder.Groups, assignments, unassignable, warnings, builder.PlaceAttachments,
            placed.Count, builder.ClusterPlaceAttachments, builder.StateThresholds);
    }

    private static void PlanRegionsAndStates(
        GroupPartitionLevelEnum level,
        Guid? rootGroupId,
        GroupPartitionContext context,
        List<PlacedClient> candidates,
        IReadOnlyList<Group> activeGroups,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var neededStates = candidates
            .Select(c => (c.Country, c.State))
            .Distinct()
            .OrderBy(s => s.Country, StringComparer.Ordinal)
            .ThenBy(s => s.State, StringComparer.Ordinal)
            .ToList();

        var regionKeyByCountryAndKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (rootGroupId is null)
        {
            var regions = neededStates
                .Select(s => (s.Country, Region: RegionFor(context, s.Country, s.State)))
                .Where(r => r.Region != null)
                .Select(r => (r.Country, Region: r.Region!))
                .DistinctBy(r => (r.Country, r.Region.Key))
                .OrderBy(r => r.Country, StringComparer.Ordinal)
                .ThenBy(r => r.Region.DisplayName, StringComparer.Ordinal);

            foreach (var (country, region) in regions)
            {
                var key = RegionKeyPrefix + country + GroupPartitionContext.StateKeySeparator + region.Key;
                var existing = LookupExistingRegionRoot(activeGroups, region, builder);
                var name = existing?.Name ?? region.DisplayName;
                builder.Groups.Add(new PlannedPartitionGroup(
                    key, name, ParentKey: null, existing != null, existing?.Id, ClientCount: 0, Method: PartitionGroupMethods.Region));
                regionKeyByCountryAndKey[country + GroupPartitionContext.StateKeySeparator + region.Key] = key;
                builder.ResolvedId[key] = existing?.Id;
            }
        }

        foreach (var (country, stateCode) in neededStates)
        {
            string? parentKey = null;
            Guid? parentActualId = rootGroupId;
            var parentIsPending = false;

            var region = rootGroupId is null ? RegionFor(context, country, stateCode) : null;
            if (region != null)
            {
                parentKey = regionKeyByCountryAndKey[country + GroupPartitionContext.StateKeySeparator + region.Key];
                parentActualId = builder.ResolvedId[parentKey];
                parentIsPending = parentActualId is null;
            }

            var key = StateKeyPrefix + country + GroupPartitionContext.StateKeySeparator + stateCode;
            var existing = LookupExisting(existingByNameAndParent, stateCode, parentActualId, parentIsPending);
            var clientCount = level == GroupPartitionLevelEnum.State
                ? candidates.Count(c => c.Country == country && string.Equals(c.State, stateCode, StringComparison.OrdinalIgnoreCase))
                : 0;
            var description = context.StateNameByCountryAndCode.TryGetValue(GroupPartitionContext.StateKey(country, stateCode), out var name)
                ? name
                : stateCode;

            if (existing == null && region != null && parentKey != null)
            {
                WarnWhenStateExistsUnderOtherRegion(
                    context, country, stateCode, region, builder.Groups.Single(g => g.Key == parentKey).Name, activeGroups, builder);
            }

            builder.Groups.Add(new PlannedPartitionGroup(
                key, stateCode, parentKey, existing != null, existing?.Id, clientCount, description,
                DirectCount: clientCount, Method: PartitionGroupMethods.State));
            builder.StateKeyByCountryAndCode[StateLookupKey(country, stateCode)] = key;
            builder.ResolvedId[key] = existing?.Id;
        }
    }

    private static Group? LookupExistingRegionRoot(IReadOnlyList<Group> activeGroups, RegionDescriptor region, PlanBuilder builder)
    {
        var names = new HashSet<string>(region.AllNames.Select(n => n.Trim()), StringComparer.OrdinalIgnoreCase);
        var hits = activeGroups
            .Where(g => g.Parent == null && names.Contains(g.Name.Trim()))
            .OrderBy(g => g.Name, StringComparer.Ordinal)
            .ToList();

        if (hits.Count <= 1)
        {
            return hits.FirstOrDefault();
        }

        builder.Warnings.Add(string.Format(
            AmbiguousRegionRootWarningTemplate,
            region.DisplayName,
            string.Join(", ", hits.Select(g => $"'{g.Name}' (id {g.Id})"))));
        builder.NamesWithSpecificWarning.Add(region.DisplayName);
        return null;
    }

    private static void WarnWhenStateExistsUnderOtherRegion(
        GroupPartitionContext context,
        string country,
        string stateCode,
        RegionDescriptor region,
        string regionGroupName,
        IReadOnlyList<Group> activeGroups,
        PlanBuilder builder)
    {
        if (!context.RegionByCountryAndState.TryGetValue(country, out var byState))
        {
            return;
        }

        var otherRegionNames = new HashSet<string>(
            byState.Values
                .Where(r => !string.Equals(r.Key, region.Key, StringComparison.Ordinal))
                .SelectMany(r => r.AllNames)
                .Select(n => n.Trim()),
            StringComparer.OrdinalIgnoreCase);
        var rootById = activeGroups.Where(g => g.Parent == null).ToDictionary(g => g.Id);

        var misplaced = activeGroups
            .Where(g => string.Equals(g.Name.Trim(), stateCode, StringComparison.OrdinalIgnoreCase)
                && g.Parent.HasValue
                && rootById.TryGetValue(g.Parent.Value, out var root)
                && otherRegionNames.Contains(root.Name.Trim()))
            .Select(g => rootById[g.Parent!.Value].Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .FirstOrDefault();

        if (misplaced == null)
        {
            return;
        }

        builder.Warnings.Add(string.Format(StateUnderOtherRegionWarningTemplate, stateCode, misplaced, regionGroupName));
        builder.NamesWithSpecificWarning.Add(stateCode);
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

            builder.Groups.Add(new PlannedPartitionGroup(
                key, city, ParentKey: null, existing != null, existing?.Id, clientCount,
                DirectCount: clientCount, Method: PartitionGroupMethods.City));
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

            builder.Groups.Add(new PlannedPartitionGroup(
                key, city, stateKey, existing != null, existing?.Id, clientCount,
                DirectCount: clientCount, Method: PartitionGroupMethods.City));
            cityKeyByStateAndCity[StateLookupKey(country, state) + GroupPartitionContext.StateKeySeparator + city] = key;
        }

        foreach (var client in placed)
        {
            builder.LeafKeyByClientId[client.Client.Id] =
                cityKeyByStateAndCity[StateLookupKey(client.Country, client.State) + GroupPartitionContext.StateKeySeparator + client.City];
        }
    }

    private static AddressClusterPlan PlanClustersUnderStates(
        int sharePercent,
        List<PlacedClient> placed,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var candidateStates = new HashSet<string>(
            placed.Where(c => c.IsCandidate).Select(c => StateLookupKey(c.Country, c.State)), StringComparer.OrdinalIgnoreCase);
        var population = placed.Where(c => candidateStates.Contains(StateLookupKey(c.Country, c.State))).ToList();
        var placedById = population.ToDictionary(c => c.Client.Id);
        var clusterPlan = AddressClusterPlanner.Plan(
            population.Select(c => new ClusterAddress(c.Client.Id, c.Country, c.State, c.City, c.Latitude, c.Longitude)).ToList(),
            sharePercent);

        var candidateAssignmentsByCluster = clusterPlan.Assignments
            .Where(a => placedById[a.ClientId].IsCandidate)
            .GroupBy(a => ClusterLookupKey(a.Country, a.State, a.City), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var candidatePlaces = new HashSet<string>(
            clusterPlan.Assignments
                .Where(a => a.AttachedByDistance && placedById[a.ClientId].IsCandidate)
                .Select(a => PlaceLookupKey(a.Country, a.State, placedById[a.ClientId].City)),
            StringComparer.OrdinalIgnoreCase);
        var placeAttachments = clusterPlan.PlaceAttachments
            .Where(p => candidatePlaces.Contains(PlaceLookupKey(p.Country, p.State, p.Place)))
            .ToList();

        var clusterKeyByCluster = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var cluster in clusterPlan.Clusters)
        {
            var stateKey = builder.StateKeyByCountryAndCode[StateLookupKey(cluster.Country, cluster.State)];
            var parentActualId = builder.ResolvedId[stateKey];
            var parentIsPending = parentActualId is null;

            var clusterLookup = ClusterLookupKey(cluster.Country, cluster.State, cluster.City);
            var key = ClusterKeyPrefix + clusterLookup;
            var existing = LookupExisting(existingByNameAndParent, cluster.City, parentActualId, parentIsPending);
            var members = candidateAssignmentsByCluster.GetValueOrDefault(clusterLookup) ?? [];
            if (members.Count == 0 && existing == null)
            {
                continue;
            }

            var attachedPlaces = placeAttachments.Count(p =>
                p.Country == cluster.Country && p.State == cluster.State && p.CentreCity == cluster.City);

            builder.Groups.Add(new PlannedPartitionGroup(
                key, cluster.City, stateKey, existing != null, existing?.Id,
                members.Count, string.Empty, cluster.Latitude, cluster.Longitude,
                DirectCount: members.Count(a => !a.AttachedByDistance),
                AttachedCount: members.Count(a => a.AttachedByDistance),
                AttachedPlaceCount: attachedPlaces,
                Method: PartitionGroupMethods.Cluster));
            clusterKeyByCluster[clusterLookup] = key;
            builder.ResolvedId[key] = existing?.Id;
        }

        foreach (var members in candidateAssignmentsByCluster.Values)
        {
            foreach (var assignment in members)
            {
                builder.LeafKeyByClientId[assignment.ClientId] =
                    clusterKeyByCluster[ClusterLookupKey(assignment.Country, assignment.State, assignment.City)];
            }
        }

        foreach (var rejection in clusterPlan.Rejections.Where(r => placedById[r.ClientId].IsCandidate))
        {
            builder.Unassignable.Add(new UnassignablePartitionClient(
                rejection.ClientId, DisplayName(placedById[rejection.ClientId].Client), rejection.Reason));
        }

        builder.ClusterPlaceAttachments.AddRange(placeAttachments);
        builder.StateThresholds.AddRange(clusterPlan.StateThresholds);

        return clusterPlan;
    }

    private static void PlanMunicipalitiesUnderClusters(
        int sharePercent,
        AddressClusterPlan clusterPlan,
        List<PlacedClient> placed,
        Dictionary<string, Group> existingByNameAndParent,
        PlanBuilder builder)
    {
        var placedById = placed.ToDictionary(c => c.Client.Id);
        var membersByCluster = clusterPlan.Assignments
            .GroupBy(a => ClusterLookupKey(a.Country, a.State, a.City), StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Select(a => placedById[a.ClientId]).ToList(), StringComparer.Ordinal);

        foreach (var cluster in clusterPlan.Clusters)
        {
            var clusterLookup = ClusterLookupKey(cluster.Country, cluster.State, cluster.City);
            var clusterKey = ClusterKeyPrefix + clusterLookup;
            if (!builder.ResolvedId.TryGetValue(clusterKey, out var parentActualId)
                || !membersByCluster.TryGetValue(clusterLookup, out var members))
            {
                continue;
            }

            var subPlan = MunicipalitySubClusterPlanner.Plan(
                cluster.City,
                cluster.Latitude,
                cluster.Longitude,
                members.Select(c => new ClusterAddress(c.Client.Id, c.Country, c.State, c.City, c.Latitude, c.Longitude)).ToList(),
                sharePercent);

            var candidatePlaces = new HashSet<string>(
                members.Where(c => c.IsCandidate).Select(c => c.City), StringComparer.OrdinalIgnoreCase);
            var keptAttachments = subPlan.Attachments.Where(a => candidatePlaces.Contains(a.Place)).ToList();
            var movedCandidates = subPlan.SubClusterByClientId
                .Where(entry => placedById[entry.Key].IsCandidate)
                .ToList();

            var parentIsPending = parentActualId is null;
            var subKeyByCity = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var subCluster in subPlan.SubClusters)
            {
                var key = MunicipalityKeyPrefix + clusterLookup + GroupPartitionContext.StateKeySeparator + subCluster.City;
                var existing = LookupExisting(existingByNameAndParent, subCluster.City, parentActualId, parentIsPending);
                var subMembers = movedCandidates.Where(entry => entry.Value == subCluster.City).ToList();
                if (subMembers.Count == 0 && existing == null)
                {
                    continue;
                }

                var direct = subMembers.Count(entry =>
                    string.Equals(placedById[entry.Key].City, subCluster.City, StringComparison.OrdinalIgnoreCase));
                builder.Groups.Add(new PlannedPartitionGroup(
                    key, subCluster.City, clusterKey, existing != null, existing?.Id,
                    subMembers.Count, string.Empty, subCluster.Latitude, subCluster.Longitude,
                    DirectCount: direct,
                    AttachedCount: subMembers.Count - direct,
                    AttachedPlaceCount: keptAttachments.Count(a => a.SubClusterCity == subCluster.City),
                    Method: PartitionGroupMethods.Municipality));
                builder.ResolvedId[key] = existing?.Id;
                subKeyByCity[subCluster.City] = key;
            }

            foreach (var (clientId, subCity) in movedCandidates)
            {
                builder.LeafKeyByClientId[clientId] = subKeyByCity[subCity];
            }

            var clusterIndex = builder.Groups.FindIndex(g => g.Key == clusterKey);
            var cityCluster = builder.Groups[clusterIndex];
            var remaining = cityCluster.ClientCount - movedCandidates.Count;
            builder.Groups[clusterIndex] = cityCluster with
            {
                ClientCount = remaining,
                AttachedCount = remaining - cityCluster.DirectCount,
                AttachedPlaceCount = keptAttachments.Count(a => a.SubClusterCity == null)
            };

            foreach (var attachment in keptAttachments)
            {
                builder.PlaceAttachments.Add(new PartitionPlaceAttachment(
                    attachment.Place,
                    cluster.City,
                    attachment.SubClusterCity ?? cluster.City,
                    attachment.SubClusterCity != null,
                    attachment.ClientCount,
                    attachment.DistanceKm));
            }
        }
    }

    private static RegionDescriptor? RegionFor(GroupPartitionContext context, string country, string stateCode) =>
        context.RegionByCountryAndState.TryGetValue(country, out var byState) && byState.TryGetValue(stateCode, out var region)
            ? region
            : null;

    private static string NormalizeCountry(string countryCode) =>
        (CountryCodeNormalizer.ToAlpha2(countryCode) ?? string.Empty).Trim().ToUpperInvariant();

    private static string StateLookupKey(string country, string state) => country + GroupPartitionContext.StateKeySeparator + state;

    private static string ClusterLookupKey(string country, string state, string city) =>
        country + GroupPartitionContext.StateKeySeparator + state + GroupPartitionContext.StateKeySeparator + city;

    private static string PlaceLookupKey(string country, string state, string place) =>
        ClusterLookupKey(country, state, place.Trim());

    private static List<string> BuildDuplicateNameWarnings(
        IReadOnlyList<PlannedPartitionGroup> groups, ILookup<string, Group> groupsByNameAnywhere, IReadOnlySet<string> alreadyWarned)
    {
        var warnings = new List<string>();
        var warnedNames = new HashSet<string>(alreadyWarned, StringComparer.OrdinalIgnoreCase);

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
