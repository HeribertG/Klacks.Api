// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the geographic customer-grouping proposal: assigns each customer to the nearest group that
/// carries coordinates (a "location group"), and determines which of the customer's current location
/// memberships must be retired so the customer moves from a coarse node (e.g. a canton) down to the
/// nearest finer node (e.g. a city). Polymorphic groups without coordinates are never targets, and
/// non-location memberships (e.g. qualification groups) are left untouched. Read-only: it computes the
/// decision; applying it is the caller's job.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Grouping;

public class CustomerGroupingPlanner : ICustomerGroupingPlanner
{
    private const string ReasonNoCoordinates = "no geocoded address";
    private const string ReasonNoAnchors = "no groups carry coordinates";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;

    public CustomerGroupingPlanner(IClientRepository clientRepository, IGroupRepository groupRepository)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
    }

    public async Task<CustomerGroupingProposal> BuildProposalAsync(CancellationToken cancellationToken = default)
    {
        var groups = (await _groupRepository.List())
            .Where(g => !g.IsDeleted)
            .ToList();
        var groupById = groups.ToDictionary(g => g.Id);

        var anchors = groups
            .Where(g => g.Latitude.HasValue && g.Longitude.HasValue)
            .Select(g => new GroupAnchor(g.Id, g.Latitude!.Value, g.Longitude!.Value))
            .ToList();
        var anchorGroupIds = anchors.Select(a => a.GroupId).ToHashSet();

        // Location-hierarchy groups: every anchor (a geocoded city) plus every group that lives in the same
        // tree (region root) as an anchor — i.e. the whole region/canton/city hierarchy that contains
        // geocoded cities. A membership in one of these is the location membership a move replaces, even
        // when the customer's own canton has no geocoded city yet, or the nearest city sits in another
        // canton. Groups in unrelated trees (e.g. qualification or hierarchy systems with their own root)
        // are left untouched.
        var anchorRoots = anchorGroupIds.Select(id => groupById[id]).Select(RootKey).ToHashSet();
        var locationGroupIds = groups
            .Where(g => anchorGroupIds.Contains(g.Id) || anchorRoots.Contains(RootKey(g)))
            .Select(g => g.Id)
            .ToHashSet();

        var customers = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            EntityTypeEnum.Customer, cancellationToken);

        var assignments = new List<CustomerGroupingAssignment>();
        var unassigned = new List<UnassignedCustomer>();

        foreach (var customer in customers)
        {
            var coordinate = customer.Addresses
                .FirstOrDefault(a => !a.IsDeleted && a.Latitude.HasValue && a.Longitude.HasValue);

            if (coordinate == null)
            {
                unassigned.Add(new UnassignedCustomer(customer.Id, DisplayName(customer), ReasonNoCoordinates));
                continue;
            }

            var nearest = CustomerGroupAssigner.FindNearest(
                new CustomerLocation(customer.Id, coordinate.Latitude!.Value, coordinate.Longitude!.Value),
                anchors);

            if (nearest == null)
            {
                unassigned.Add(new UnassignedCustomer(customer.Id, DisplayName(customer), ReasonNoAnchors));
                continue;
            }

            var targetGroup = groupById[nearest.GroupId];
            var activeMemberships = customer.GroupItems.Where(gi => !gi.IsDeleted).ToList();

            var alreadyInTarget = activeMemberships.Any(gi => gi.GroupId == targetGroup.Id);
            var retireGroupIds = activeMemberships
                .Where(gi => gi.GroupId != targetGroup.Id && locationGroupIds.Contains(gi.GroupId))
                .Select(gi => gi.GroupId)
                .Distinct()
                .ToList();

            if (alreadyInTarget && retireGroupIds.Count == 0)
            {
                continue;
            }

            var currentGroupNames = activeMemberships
                .Where(gi => groupById.ContainsKey(gi.GroupId))
                .Select(gi => groupById[gi.GroupId].Name)
                .ToList();

            assignments.Add(new CustomerGroupingAssignment(
                customer.Id,
                DisplayName(customer),
                currentGroupNames,
                targetGroup.Id,
                targetGroup.Name,
                nearest.DistanceKm,
                retireGroupIds));
        }

        return new CustomerGroupingProposal(anchors.Count, assignments, unassigned);
    }

    private static Guid RootKey(Group group) => group.Root ?? group.Id;

    private static string DisplayName(Client client)
    {
        var name = $"{client.FirstName} {client.Name}".Trim();
        return string.IsNullOrWhiteSpace(name) ? client.Name : name;
    }
}
