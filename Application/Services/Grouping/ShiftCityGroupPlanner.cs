// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure, read-only planner for assign_shifts_to_city_groups: derives the city group (a leaf of the group
/// tree) of every shift from its customer's address, in a fixed precedence — the customer's own active
/// membership in a city group, then a city group whose name exactly equals the address city, then, among
/// the city groups below the group named after the address state (all city groups when there is none),
/// a group whose name starts with the city or the city with the group name, then the only candidate,
/// then the nearest candidate, then the nearest city group overall. Region and state nodes are never a
/// target. A city group's location is its own coordinates, else the mean position of every geocoded
/// address whose city matches its name; city groups with neither are reported, because a nearest match
/// cannot pick them. Shifts that already hold a city-group link are skipped, which
/// makes a second run change nothing; for every other shift all current links are replaced and their
/// validity window is carried over to the new link. It never touches the database.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Services.Orders;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Grouping;

public static class ShiftCityGroupPlanner
{
    private const string ReasonNoCustomer = "the shift has no customer";
    private const string ReasonNoUsableAddress = "the customer has no address with a city or coordinates";
    private const string ReasonNoCityGroups = "the group tree has no city groups";
    private const string ReasonNoMatch = "no city group is named after the customer's city and the customer's address has no coordinates";
    private const string ReasonNoLocatedGroup = "no city group is named after the customer's city and no city group has a known location";

    private const string MatchByMembership = "customer's city-group membership";
    private const string MatchByCityName = "city name";
    private const string MatchByPartialCityName = "city name contained in the group name";
    private const string MatchByOnlyCandidate = "only city group of the customer's state";
    private const string MatchByNearestInState = "nearest city group in the customer's state";
    private const string MatchByNearest = "nearest city group";

    public static ShiftCityGroupPlan Plan(
        IReadOnlyList<Shift> shifts,
        IReadOnlyList<Group> groups,
        IReadOnlyList<CityCentroid> cityCentroids,
        DateTime today,
        int? maxCount = null)
    {
        var activeGroups = groups.Where(g => !g.IsDeleted).ToList();
        var cityGroups = activeGroups.Where(IsLeaf).ToList();
        var cityGroupIds = cityGroups.Select(g => g.Id).ToHashSet();
        var groupById = activeGroups.ToDictionary(g => g.Id);
        var groupsByUniqueName = CustomerGroupingPlanner.BuildUniqueNameIndex(activeGroups);
        var locations = BuildLocations(cityGroups, cityCentroids);

        var skipped = 0;
        var assignments = new List<ShiftCityGroupAssignment>();
        var unassignable = new List<UnassignableShift>();

        foreach (var shift in shifts)
        {
            var currentLinks = shift.GroupItems
                .Where(gi => !gi.IsDeleted && gi.AnalyseToken == null)
                .ToList();

            if (currentLinks.Any(gi => cityGroupIds.Contains(gi.GroupId)))
            {
                skipped++;
                continue;
            }

            if (maxCount.HasValue && assignments.Count + unassignable.Count >= maxCount.Value)
            {
                break;
            }

            var customer = shift.Client;
            if (customer == null)
            {
                unassignable.Add(new UnassignableShift(shift.Id, shift.Name, string.Empty, ReasonNoCustomer));
                continue;
            }

            var customerName = OrderGroupPlanner.DisplayName(customer);

            if (cityGroups.Count == 0)
            {
                unassignable.Add(new UnassignableShift(shift.Id, shift.Name, customerName, ReasonNoCityGroups));
                continue;
            }

            string? reason = null;
            var match = ResolveMembership(customer, cityGroupIds, groupById, today)
                ?? ResolveByAddress(customer, cityGroups, groupsByUniqueName, locations, out reason);

            if (match == null)
            {
                unassignable.Add(new UnassignableShift(shift.Id, shift.Name, customerName, reason!));
                continue;
            }

            assignments.Add(new ShiftCityGroupAssignment(
                shift.Id,
                shift.Name,
                shift.Status,
                customerName,
                match.Value.Group.Id,
                match.Value.Group.Name,
                match.Value.Reason,
                match.Value.DistanceKm,
                currentLinks.Select(gi => gi.Id).ToList(),
                currentLinks
                    .Select(gi => groupById.TryGetValue(gi.GroupId, out var g) ? g.Name : gi.GroupId.ToString())
                    .ToList(),
                MergeValidFrom(currentLinks),
                MergeValidUntil(currentLinks)));
        }

        var unlocated = cityGroups
            .Where(g => !locations.ContainsKey(g.Id))
            .Select(g => g.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        return new ShiftCityGroupPlan(shifts.Count, skipped, assignments, unassignable, unlocated);
    }

    internal static bool IsLeaf(Group group) => group.Rgt - group.Lft == 1;

    internal static bool IsDescendantOf(Group candidate, Group ancestor)
    {
        return (candidate.Root ?? candidate.Id) == (ancestor.Root ?? ancestor.Id)
            && candidate.Lft > ancestor.Lft
            && candidate.Rgt < ancestor.Rgt;
    }

    internal static bool IsPartialNameMatch(string city, string groupName)
    {
        var normalizedCity = Normalize(city);
        var normalizedGroup = Normalize(groupName);
        if (normalizedCity.Length == 0 || normalizedGroup.Length == 0)
        {
            return false;
        }

        return normalizedCity == normalizedGroup
            || StartsWithWord(normalizedGroup, normalizedCity)
            || StartsWithWord(normalizedCity, normalizedGroup);
    }

    private static (Group Group, string Reason, double? DistanceKm)? ResolveMembership(
        Client customer, HashSet<Guid> cityGroupIds, IReadOnlyDictionary<Guid, Group> groupById, DateTime today)
    {
        var group = customer.GroupItems
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == null && cityGroupIds.Contains(gi.GroupId))
            .Where(gi => !gi.ValidUntil.HasValue || gi.ValidUntil.Value.Date >= today.Date)
            .Where(gi => !gi.ValidFrom.HasValue || gi.ValidFrom.Value.Date <= today.Date)
            .OrderByDescending(gi => gi.ValidFrom)
            .ThenBy(gi => gi.GroupId)
            .Select(gi => groupById[gi.GroupId])
            .FirstOrDefault();

        return group == null ? null : (group, MatchByMembership, (double?)null);
    }

    private static (Group Group, string Reason, double? DistanceKm)? ResolveByAddress(
        Client customer,
        IReadOnlyList<Group> cityGroups,
        IReadOnlyDictionary<string, Group> groupsByUniqueName,
        IReadOnlyDictionary<Guid, GroupAnchor> locations,
        out string? reason)
    {
        reason = null;
        var address = OrderGroupPlanner.SelectOrderAddress(
            customer, a => CustomerGroupingPlanner.HasCity(a) || CustomerGroupingPlanner.HasCoordinates(a));
        if (address == null)
        {
            reason = ReasonNoUsableAddress;
            return null;
        }

        var addressLabel = OrderGroupPlanner.AddressLabel(address.Type);
        var city = address.City.Trim();

        if (city.Length > 0
            && groupsByUniqueName.TryGetValue(city, out var exact)
            && IsLeaf(exact))
        {
            return (exact, Describe(MatchByCityName, addressLabel), null);
        }

        var stateGroup = ResolveStateGroup(address.State, groupsByUniqueName);
        var (candidates, narrowedToState) = ResolveCandidates(stateGroup, cityGroups);

        if (city.Length > 0)
        {
            var partial = candidates.Where(g => IsPartialNameMatch(city, g.Name)).ToList();
            if (partial.Count == 1)
            {
                return (partial[0], Describe(MatchByPartialCityName, addressLabel), null);
            }
        }

        if (narrowedToState && candidates.Count == 1)
        {
            return (candidates[0], Describe(MatchByOnlyCandidate, addressLabel), null);
        }

        if (!CustomerGroupingPlanner.HasCoordinates(address))
        {
            reason = ReasonNoMatch;
            return null;
        }

        var nearest = FindNearest(customer.Id, address, candidates, locations);
        if (nearest != null)
        {
            var label = narrowedToState ? MatchByNearestInState : MatchByNearest;
            return (nearest.Value.Group, Describe(label, addressLabel), nearest.Value.DistanceKm);
        }

        if (narrowedToState)
        {
            var overall = FindNearest(customer.Id, address, cityGroups, locations);
            if (overall != null)
            {
                return (overall.Value.Group, Describe(MatchByNearest, addressLabel), overall.Value.DistanceKm);
            }
        }

        reason = ReasonNoLocatedGroup;
        return null;
    }

    private static Group? ResolveStateGroup(string state, IReadOnlyDictionary<string, Group> groupsByUniqueName)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        return groupsByUniqueName.TryGetValue(state.Trim(), out var group) ? group : null;
    }

    private static (List<Group> Candidates, bool NarrowedToState) ResolveCandidates(
        Group? stateGroup, IReadOnlyList<Group> cityGroups)
    {
        if (stateGroup == null)
        {
            return (cityGroups.ToList(), false);
        }

        if (IsLeaf(stateGroup))
        {
            return ([stateGroup], true);
        }

        var below = cityGroups.Where(g => IsDescendantOf(g, stateGroup)).ToList();
        return below.Count > 0 ? (below, true) : (cityGroups.ToList(), false);
    }

    private static (Group Group, double DistanceKm)? FindNearest(
        Guid customerId, Address address, IReadOnlyList<Group> candidates, IReadOnlyDictionary<Guid, GroupAnchor> locations)
    {
        var anchors = candidates
            .Where(g => locations.ContainsKey(g.Id))
            .Select(g => locations[g.Id])
            .ToList();

        var nearest = CustomerGroupAssigner.FindNearest(
            new CustomerLocation(customerId, address.Latitude!.Value, address.Longitude!.Value), anchors);
        if (nearest == null)
        {
            return null;
        }

        return (candidates.First(g => g.Id == nearest.GroupId), nearest.DistanceKm);
    }

    private static Dictionary<Guid, GroupAnchor> BuildLocations(
        IReadOnlyList<Group> cityGroups, IReadOnlyList<CityCentroid> cityCentroids)
    {
        var locations = new Dictionary<Guid, GroupAnchor>();

        foreach (var group in cityGroups)
        {
            if (group.Latitude.HasValue && group.Longitude.HasValue)
            {
                locations[group.Id] = new GroupAnchor(group.Id, group.Latitude.Value, group.Longitude.Value);
                continue;
            }

            var matching = cityCentroids.Where(c => Normalize(c.City) == Normalize(group.Name)).ToList();
            if (matching.Count == 0)
            {
                matching = cityCentroids.Where(c => IsPartialNameMatch(c.City, group.Name)).ToList();
            }

            var addressCount = matching.Sum(c => c.AddressCount);
            if (addressCount == 0)
            {
                continue;
            }

            locations[group.Id] = new GroupAnchor(
                group.Id,
                matching.Sum(c => c.Latitude * c.AddressCount) / addressCount,
                matching.Sum(c => c.Longitude * c.AddressCount) / addressCount);
        }

        return locations;
    }

    private static DateTime? MergeValidFrom(IReadOnlyList<GroupItem> links)
    {
        if (links.Count == 0 || links.Any(gi => !gi.ValidFrom.HasValue))
        {
            return null;
        }

        return links.Min(gi => gi.ValidFrom);
    }

    private static DateTime? MergeValidUntil(IReadOnlyList<GroupItem> links)
    {
        if (links.Count == 0 || links.Any(gi => !gi.ValidUntil.HasValue))
        {
            return null;
        }

        return links.Max(gi => gi.ValidUntil);
    }

    private static bool StartsWithWord(string text, string prefix)
    {
        return text.Length > prefix.Length
            && text.StartsWith(prefix, StringComparison.Ordinal)
            && !char.IsLetterOrDigit(text[prefix.Length]);
    }

    private static string Normalize(string value)
    {
        return string.Join(' ', value.Trim().ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string Describe(string matchKind, string addressLabel) => $"{matchKind} ({addressLabel})";

}
