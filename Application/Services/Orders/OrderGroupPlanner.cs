// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure, read-only planner for assign_orders_to_groups: derives the target group of every open order
/// from its customer, in a fixed precedence — the customer's own active membership in a group carrying
/// coordinates (the cluster the address partition placed it in), then exact city-name match, then exact
/// state-code match, then the nearest group carrying coordinates, then unassigned with a reason. It never
/// touches the database; the command handler is the only place that writes. Address choice differs from
/// the staff planners in one point: an order is fulfilled where the customer works, so the workplace
/// address wins over the main address, and only below that the same rule as CustomerGroupingPlanner
/// applies. Scenario rows are the caller's responsibility to exclude; scenario memberships (AnalyseToken
/// set) are ignored here so an analysis scenario never hides a needed placement.
/// </summary>

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Orders;

public static class OrderGroupPlanner
{
    private const AddressTypeEnum PreferredOrderAddressType = AddressTypeEnum.Workplace;

    private const string ReasonNoCustomer = "the order has no customer";
    private const string ReasonNoUsableAddress = "the customer has no address with a city, a state or coordinates";
    private const string ReasonNoAnchors = "no group is named after the customer's city or state and no group carries coordinates";
    private const string ReasonNoMatch = "no group is named after the customer's city or state and the customer's address has no coordinates";

    private const string MatchByMembership = "customer's group membership";
    private const string MatchByCityName = "city name";
    private const string MatchByStateCode = "state code";
    private const string MatchByCoordinates = "nearest coordinates";

    private const string MainAddressLabel = "main address";
    private const string WorkplaceAddressLabel = "workplace address";
    private const string InvoicingAddressLabel = "invoicing address";

    public static OrderGroupPlan Plan(IReadOnlyList<Shift> orders, IReadOnlyList<Group> groups, DateTime today)
    {
        var activeGroups = groups.Where(g => !g.IsDeleted).ToList();
        var groupById = activeGroups.ToDictionary(g => g.Id);
        var groupsByUniqueName = CustomerGroupingPlanner.BuildUniqueNameIndex(activeGroups);
        var geoAnchors = activeGroups
            .Where(g => g.Latitude.HasValue && g.Longitude.HasValue)
            .Select(g => new GroupAnchor(g.Id, g.Latitude!.Value, g.Longitude!.Value))
            .ToList();

        var skipped = 0;
        var assignments = new List<OrderGroupAssignment>();
        var unassignable = new List<UnassignableOrder>();

        foreach (var order in orders)
        {
            if (order.GroupItems.Any(gi => !gi.IsDeleted && gi.AnalyseToken == null))
            {
                skipped++;
                continue;
            }

            var customer = order.Client;
            if (customer == null)
            {
                unassignable.Add(new UnassignableOrder(order.Id, order.Name, string.Empty, ReasonNoCustomer));
                continue;
            }

            var customerName = DisplayName(customer);

            var membershipTarget = ResolveMembershipTarget(customer, groupById, today);
            if (membershipTarget != null)
            {
                assignments.Add(new OrderGroupAssignment(
                    order.Id, order.Name, customerName, membershipTarget.Id, membershipTarget.Name, MatchByMembership, null));
                continue;
            }

            var cityAddress = SelectOrderAddress(customer, CustomerGroupingPlanner.HasCity);
            var stateAddress = SelectOrderAddress(customer, HasState);
            var coordinateAddress = SelectOrderAddress(customer, CustomerGroupingPlanner.HasCoordinates);

            var nameTarget = ResolveNameTarget(cityAddress?.City, groupsByUniqueName);
            if (nameTarget != null)
            {
                assignments.Add(new OrderGroupAssignment(
                    order.Id, order.Name, customerName, nameTarget.Id, nameTarget.Name,
                    DescribeMatch(MatchByCityName, cityAddress!.Type), null));
                continue;
            }

            var stateTarget = ResolveNameTarget(stateAddress?.State, groupsByUniqueName);
            if (stateTarget != null)
            {
                assignments.Add(new OrderGroupAssignment(
                    order.Id, order.Name, customerName, stateTarget.Id, stateTarget.Name,
                    DescribeMatch(MatchByStateCode, stateAddress!.Type), null));
                continue;
            }

            if (coordinateAddress != null && geoAnchors.Count > 0)
            {
                var nearest = CustomerGroupAssigner.FindNearest(
                    new CustomerLocation(
                        customer.Id,
                        coordinateAddress.Latitude!.Value,
                        coordinateAddress.Longitude!.Value),
                    geoAnchors)!;

                var nearestGroup = groupById[nearest.GroupId];
                assignments.Add(new OrderGroupAssignment(
                    order.Id, order.Name, customerName, nearestGroup.Id, nearestGroup.Name,
                    DescribeMatch(MatchByCoordinates, coordinateAddress.Type), nearest.DistanceKm));
                continue;
            }

            unassignable.Add(new UnassignableOrder(
                order.Id, order.Name, customerName,
                ResolveUnassignableReason(cityAddress, stateAddress, coordinateAddress, geoAnchors.Count > 0)));
        }

        return new OrderGroupPlan(orders.Count, skipped, assignments, unassignable);
    }

    internal static Address? SelectOrderAddress(Client customer, Func<Address, bool> isUsable)
    {
        var workplace = customer.Addresses
            .Where(a => !a.IsDeleted && a.Type == PreferredOrderAddressType && isUsable(a))
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault();

        return workplace ?? CustomerGroupingPlanner.SelectPreferredAddress(customer, isUsable);
    }

    private static bool HasState(Address address) => !string.IsNullOrWhiteSpace(address.State);

    private static Group? ResolveMembershipTarget(Client customer, IReadOnlyDictionary<Guid, Group> groupById, DateTime today)
    {
        return customer.GroupItems
            .Where(gi => !gi.IsDeleted && gi.AnalyseToken == null)
            .Where(gi => !gi.ValidUntil.HasValue || gi.ValidUntil.Value.Date >= today.Date)
            .Where(gi => !gi.ValidFrom.HasValue || gi.ValidFrom.Value.Date <= today.Date)
            .OrderByDescending(gi => gi.ValidFrom)
            .Select(gi => groupById.TryGetValue(gi.GroupId, out var group) ? group : null)
            .FirstOrDefault(group => group != null && group.Latitude.HasValue && group.Longitude.HasValue);
    }

    private static Group? ResolveNameTarget(string? candidate, IReadOnlyDictionary<string, Group> groupsByUniqueName)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        return groupsByUniqueName.TryGetValue(candidate.Trim(), out var group) ? group : null;
    }

    private static string ResolveUnassignableReason(
        Address? cityAddress, Address? stateAddress, Address? coordinateAddress, bool hasGeoAnchors)
    {
        if (cityAddress == null && stateAddress == null && coordinateAddress == null)
        {
            return ReasonNoUsableAddress;
        }

        return hasGeoAnchors ? ReasonNoMatch : ReasonNoAnchors;
    }

    private static string DescribeMatch(string matchKind, AddressTypeEnum addressType)
        => $"{matchKind} ({AddressLabel(addressType)})";

    private static string AddressLabel(AddressTypeEnum addressType) => addressType switch
    {
        AddressTypeEnum.Workplace => WorkplaceAddressLabel,
        AddressTypeEnum.InvoicingAddress => InvoicingAddressLabel,
        _ => MainAddressLabel
    };

    private static string DisplayName(Client customer)
    {
        if (!string.IsNullOrWhiteSpace(customer.Company))
        {
            return customer.Company!;
        }

        var name = $"{customer.FirstName} {customer.Name}".Trim();
        return string.IsNullOrWhiteSpace(name) ? customer.Name : name;
    }
}
