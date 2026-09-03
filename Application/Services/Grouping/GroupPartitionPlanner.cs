// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, read-only planner for partition_clients_by_address: turns a client list and the currently
/// existing groups into the region/canton/city hierarchy the skill would create or reuse, plus the
/// per-client leaf placement. It never touches the database — the caller (the command handler) is the
/// only place that writes. The "current address" of a client is resolved the same way
/// <see cref="CustomerGroupingPlanner"/> does (Employee-type address preferred, then the most recently
/// valid one of any type), so this planner and the geographic customer-grouping feature agree on what
/// "the client's address" means. Region parents mirror the deterministic canton-to-region assignment
/// baked into GroupsSeed (see <see cref="SwissCantonRegions"/>) unless a caller-supplied root group
/// overrides it, in which case every canton (or, at City level, every city) attaches directly under
/// that root instead.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Grouping;

public static class GroupPartitionPlanner
{
    private const string ReasonNoAddress = "no address on record";
    private const string ReasonNoCanton = "address has no canton (state)";
    private const string ReasonNoCity = "address has no city";
    private const string ReasonNoCantonAndCity = "address has neither canton nor city";

    private const string RegionKeyPrefix = "region:";
    private const string CantonKeyPrefix = "canton:";
    private const string CityKeyPrefix = "city:";
    private const string CityKeyPartSeparator = "|";
    private const string NameParentKeySeparator = "|group-key|";
    private const string RootParentMarker = "root";

    public static GroupPartitionPlan Plan(
        IReadOnlyList<Client> clients,
        IReadOnlyList<Group> existingGroups,
        GroupPartitionLevelEnum level,
        Guid? rootGroupId,
        bool includeAlreadyGrouped)
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
        var placedClients = new List<(Client Client, string Canton, string City)>();

        foreach (var client in clients)
        {
            if (!includeAlreadyGrouped && HasActiveMembership(client))
            {
                skipped++;
                continue;
            }

            var address = CustomerGroupingPlanner.SelectPreferredAddress(client, _ => true);
            var canton = address?.State?.Trim().ToUpperInvariant() ?? string.Empty;
            var city = address?.City?.Trim() ?? string.Empty;

            var reason = ResolveUnassignableReason(level, address, canton, city);
            if (reason != null)
            {
                unassignable.Add(new UnassignablePartitionClient(client.Id, DisplayName(client), reason));
                continue;
            }

            placedClients.Add((client, canton, city));
        }

        var groups = new List<PlannedPartitionGroup>();
        var resolvedId = new Dictionary<string, Guid?>(StringComparer.Ordinal);
        var cantonKeyByCode = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cityKeyByCityOnly = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var cityKeyByCantonAndCity = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (level != GroupPartitionLevelEnum.City)
        {
            var neededCantons = placedClients
                .Select(c => c.Canton)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.Ordinal)
                .ToList();

            var regionKeyByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (rootGroupId is null)
            {
                var regionNames = neededCantons
                    .Select(c => SwissCantonRegions.ByCantonCode.TryGetValue(c, out var region) ? region : null)
                    .Where(region => region != null)
                    .Select(region => region!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(region => region, StringComparer.Ordinal);

                foreach (var regionName in regionNames)
                {
                    var key = RegionKeyPrefix + regionName;
                    var existing = LookupExisting(existingByNameAndParent, regionName, parentActualId: null, parentIsPending: false);
                    groups.Add(new PlannedPartitionGroup(key, regionName, ParentKey: null, existing != null, existing?.Id, ClientCount: 0));
                    regionKeyByName[regionName] = key;
                    resolvedId[key] = existing?.Id;
                }
            }

            foreach (var cantonCode in neededCantons)
            {
                string? parentKey = null;
                Guid? parentActualId = rootGroupId;
                var parentIsPending = false;

                if (rootGroupId is null && SwissCantonRegions.ByCantonCode.TryGetValue(cantonCode, out var regionName))
                {
                    parentKey = regionKeyByName[regionName];
                    parentActualId = resolvedId[parentKey];
                    parentIsPending = parentActualId is null;
                }

                var key = CantonKeyPrefix + cantonCode;
                var existing = LookupExisting(existingByNameAndParent, cantonCode, parentActualId, parentIsPending);
                var clientCount = level == GroupPartitionLevelEnum.Canton
                    ? placedClients.Count(c => string.Equals(c.Canton, cantonCode, StringComparison.OrdinalIgnoreCase))
                    : 0;

                groups.Add(new PlannedPartitionGroup(key, cantonCode, parentKey, existing != null, existing?.Id, clientCount));
                cantonKeyByCode[cantonCode] = key;
                resolvedId[key] = existing?.Id;
            }
        }

        if (level == GroupPartitionLevelEnum.CantonCity)
        {
            var neededCities = placedClients
                .GroupBy(c => (Canton: c.Canton, CityUpper: c.City.ToUpperInvariant()))
                .Select(g => (Canton: g.Key.Canton, City: g.First().City))
                .OrderBy(c => c.Canton, StringComparer.Ordinal)
                .ThenBy(c => c.City, StringComparer.Ordinal);

            foreach (var (canton, city) in neededCities)
            {
                var cantonKey = cantonKeyByCode[canton];
                var parentActualId = resolvedId[cantonKey];
                var parentIsPending = parentActualId is null;

                var key = CityKeyPrefix + canton + CityKeyPartSeparator + city;
                var existing = LookupExisting(existingByNameAndParent, city, parentActualId, parentIsPending);
                var clientCount = placedClients.Count(c =>
                    string.Equals(c.Canton, canton, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));

                groups.Add(new PlannedPartitionGroup(key, city, cantonKey, existing != null, existing?.Id, clientCount));
                cityKeyByCantonAndCity[canton + CityKeyPartSeparator + city] = key;
            }
        }
        else if (level == GroupPartitionLevelEnum.City)
        {
            var neededCities = placedClients
                .Select(c => c.City)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.Ordinal);

            foreach (var city in neededCities)
            {
                var key = CityKeyPrefix + city;
                var existing = LookupExisting(existingByNameAndParent, city, rootGroupId, parentIsPending: false);
                var clientCount = placedClients.Count(c => string.Equals(c.City, city, StringComparison.OrdinalIgnoreCase));

                groups.Add(new PlannedPartitionGroup(key, city, ParentKey: null, existing != null, existing?.Id, clientCount));
                cityKeyByCityOnly[city] = key;
            }
        }

        var assignments = placedClients
            .Select(c => new PartitionClientAssignment(
                c.Client.Id,
                DisplayName(c.Client),
                LeafKeyFor(level, c.Canton, c.City, cantonKeyByCode, cityKeyByCityOnly, cityKeyByCantonAndCity)))
            .ToList();

        var warnings = BuildDuplicateNameWarnings(groups, groupsByNameAnywhere);

        return new GroupPartitionPlan(clients.Count, skipped, groups, assignments, unassignable, warnings);
    }

    private static string LeafKeyFor(
        GroupPartitionLevelEnum level,
        string canton,
        string city,
        Dictionary<string, string> cantonKeyByCode,
        Dictionary<string, string> cityKeyByCityOnly,
        Dictionary<string, string> cityKeyByCantonAndCity) => level switch
    {
        GroupPartitionLevelEnum.Canton => cantonKeyByCode[canton],
        GroupPartitionLevelEnum.City => cityKeyByCityOnly[city],
        _ => cityKeyByCantonAndCity[canton + CityKeyPartSeparator + city]
    };

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
        GroupPartitionLevelEnum level, Address? address, string canton, string city)
    {
        if (address == null)
        {
            return ReasonNoAddress;
        }

        return level switch
        {
            GroupPartitionLevelEnum.Canton => string.IsNullOrEmpty(canton) ? ReasonNoCanton : null,
            GroupPartitionLevelEnum.City => string.IsNullOrEmpty(city) ? ReasonNoCity : null,
            _ => string.IsNullOrEmpty(canton) && string.IsNullOrEmpty(city)
                ? ReasonNoCantonAndCity
                : string.IsNullOrEmpty(canton)
                    ? ReasonNoCanton
                    : string.IsNullOrEmpty(city)
                        ? ReasonNoCity
                        : null
        };
    }

    private static string DisplayName(Client client)
    {
        var name = $"{client.FirstName} {client.Name}".Trim();
        return string.IsNullOrWhiteSpace(name) ? client.Name : name;
    }
}
