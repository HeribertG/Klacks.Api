// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the grouping proposal for clients of a given entity type (customers by default, or employees /
/// external employees). Every client is placed with a fixed precedence: first an exact city-name match
/// (the city of the client's address equals the name of exactly one group, trimmed, case-insensitive),
/// and only when no name matches the nearest group carrying coordinates. The planner also determines
/// which of the client's current location memberships must be retired so the client moves from a coarse
/// node (e.g. a canton) down to the finer node (e.g. a city); non-location memberships (e.g. qualification
/// groups) are left untouched. Only real memberships count: scenario memberships (AnalyseToken set) are
/// ignored everywhere, so an analysis scenario neither hides a needed move nor gets retired by one.
/// Read-only: it computes the decision, applying it is the caller's job.
/// Two consequences are non-obvious: the duplicate-name guard runs across all groups, so a non-location
/// group that happens to carry a city name suppresses that city match for everyone (safe direction — no
/// wrong assignment), and a group matched by name pulls its own tree into the set of retirable location
/// memberships, which is what lets the coarse node be retired when no group carries coordinates at all.
/// </summary>

using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Services.Grouping;

public class CustomerGroupingPlanner : ICustomerGroupingPlanner
{
    private const AddressTypeEnum MainAddressType = AddressTypeEnum.Employee;

    private const string ReasonNoUsableAddress = "no address with a city or coordinates";
    private const string ReasonNoAnchors = "no group carries coordinates and no group name matches an address city";
    private const string ReasonNoGeoAnchors = "address city matches no group name and no group carries coordinates";
    private const string ReasonNoMatch = "address city matches no group name and the address has no coordinates";

    private const string MatchByCityName = "city name";
    private const string MatchByCoordinates = "nearest coordinates";
    private const string MainAddressLabel = "main address";
    private const string WorkplaceAddressLabel = "workplace address";
    private const string InvoicingAddressLabel = "invoicing address";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;

    public CustomerGroupingPlanner(IClientRepository clientRepository, IGroupRepository groupRepository)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
    }

    public async Task<CustomerGroupingProposal> BuildProposalAsync(
        EntityTypeEnum entityType = EntityTypeEnum.Customer,
        CancellationToken cancellationToken = default)
    {
        var groups = (await _groupRepository.List())
            .Where(g => !g.IsDeleted)
            .ToList();
        var groupById = groups.ToDictionary(g => g.Id);

        var geoAnchors = groups
            .Where(g => g.Latitude.HasValue && g.Longitude.HasValue)
            .Select(g => new GroupAnchor(g.Id, g.Latitude!.Value, g.Longitude!.Value))
            .ToList();

        var groupsByUniqueName = groups
            .Where(g => !string.IsNullOrWhiteSpace(g.Name))
            .GroupBy(g => g.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(byName => byName.Count() == 1)
            .ToDictionary(byName => byName.Key, byName => byName.First(), StringComparer.OrdinalIgnoreCase);

        var clients = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            entityType, cancellationToken);

        var candidates = clients
            .Select(client =>
            {
                var cityAddress = SelectPreferredAddress(client, HasCity);
                return (
                    Client: client,
                    CityAddress: cityAddress,
                    CoordinateAddress: SelectPreferredAddress(client, HasCoordinates),
                    NameTarget: ResolveNameTarget(cityAddress, groupsByUniqueName));
            })
            .ToList();

        var anchorGroupIds = geoAnchors
            .Select(a => a.GroupId)
            .Concat(candidates.Where(c => c.NameTarget != null).Select(c => c.NameTarget!.Id))
            .ToHashSet();

        // Location-hierarchy groups: every anchor plus every group that lives in the same tree (region root)
        // as an anchor. A membership in one of these is the location membership a move replaces, even when
        // the client's own canton has no anchor of its own. Groups in unrelated trees (e.g. qualification
        // systems with their own root) are left untouched.
        var anchorRoots = anchorGroupIds.Select(id => groupById[id]).Select(RootKey).ToHashSet();
        var locationGroupIds = groups
            .Where(g => anchorGroupIds.Contains(g.Id) || anchorRoots.Contains(RootKey(g)))
            .Select(g => g.Id)
            .ToHashSet();

        var assignments = new List<CustomerGroupingAssignment>();
        var unassigned = new List<UnassignedCustomer>();

        foreach (var candidate in candidates)
        {
            var client = candidate.Client;
            Group targetGroup;
            double? distanceKm = null;
            string matchReason;

            if (candidate.NameTarget != null)
            {
                targetGroup = candidate.NameTarget;
                matchReason = DescribeMatch(MatchByCityName, candidate.CityAddress!.Type);
            }
            else if (candidate.CoordinateAddress != null && geoAnchors.Count > 0)
            {
                var nearest = CustomerGroupAssigner.FindNearest(
                    new CustomerLocation(
                        client.Id,
                        candidate.CoordinateAddress.Latitude!.Value,
                        candidate.CoordinateAddress.Longitude!.Value),
                    geoAnchors)!;

                targetGroup = groupById[nearest.GroupId];
                distanceKm = nearest.DistanceKm;
                matchReason = DescribeMatch(MatchByCoordinates, candidate.CoordinateAddress.Type);
            }
            else
            {
                unassigned.Add(new UnassignedCustomer(
                    client.Id,
                    DisplayName(client),
                    ResolveUnassignedReason(
                        candidate.CityAddress,
                        candidate.CoordinateAddress,
                        geoAnchors.Count > 0,
                        anchorGroupIds.Count > 0)));
                continue;
            }

            var activeMemberships = client.GroupItems
                .Where(gi => !gi.IsDeleted && gi.AnalyseToken == null)
                .ToList();

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

            var retireGroupNames = retireGroupIds
                .Select(id => groupById[id].Name)
                .ToList();

            var currentGroupNames = activeMemberships
                .Where(gi => groupById.ContainsKey(gi.GroupId))
                .Select(gi => groupById[gi.GroupId].Name)
                .ToList();

            assignments.Add(new CustomerGroupingAssignment(
                client.Id,
                DisplayName(client),
                currentGroupNames,
                targetGroup.Id,
                targetGroup.Name,
                distanceKm,
                retireGroupIds,
                retireGroupNames,
                matchReason));
        }

        return new CustomerGroupingProposal(anchorGroupIds.Count, assignments, unassigned);
    }

    private static Group? ResolveNameTarget(Address? cityAddress, IReadOnlyDictionary<string, Group> groupsByUniqueName)
    {
        if (cityAddress == null)
        {
            return null;
        }

        return groupsByUniqueName.TryGetValue(cityAddress.City.Trim(), out var group) ? group : null;
    }

    private static Address? SelectPreferredAddress(Client client, Func<Address, bool> isUsable)
    {
        var usable = client.Addresses.Where(a => !a.IsDeleted && isUsable(a)).ToList();

        return usable
                   .Where(a => a.Type == MainAddressType)
                   .OrderByDescending(a => a.ValidFrom)
                   .FirstOrDefault()
               ?? usable
                   .OrderByDescending(a => a.ValidFrom)
                   .FirstOrDefault();
    }

    private static bool HasCity(Address address) => !string.IsNullOrWhiteSpace(address.City);

    private static bool HasCoordinates(Address address) => address.Latitude.HasValue && address.Longitude.HasValue;

    private static string ResolveUnassignedReason(
        Address? cityAddress, Address? coordinateAddress, bool hasGeoAnchors, bool hasAnyAnchor)
    {
        if (cityAddress == null && coordinateAddress == null)
        {
            return ReasonNoUsableAddress;
        }

        if (!hasAnyAnchor)
        {
            return ReasonNoAnchors;
        }

        return coordinateAddress != null && !hasGeoAnchors ? ReasonNoGeoAnchors : ReasonNoMatch;
    }

    private static string DescribeMatch(string matchKind, AddressTypeEnum addressType)
        => $"{matchKind} ({AddressLabel(addressType)})";

    private static string AddressLabel(AddressTypeEnum addressType) => addressType switch
    {
        AddressTypeEnum.Workplace => WorkplaceAddressLabel,
        AddressTypeEnum.InvoicingAddress => InvoicingAddressLabel,
        _ => MainAddressLabel
    };

    private static Guid RootKey(Group group) => group.Root ?? group.Id;

    private static string DisplayName(Client client)
    {
        var name = $"{client.FirstName} {client.Name}".Trim();
        return string.IsNullOrWhiteSpace(name) ? client.Name : name;
    }
}
