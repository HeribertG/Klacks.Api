// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that adds a client (customer or external employee) to the group geographically nearest to the
/// client's address. A group is a candidate only when it carries coordinates derived from its name
/// (geocoded city/village via geocode_location_groups); groups whose name does not resolve to a place are
/// skipped. When the client has no geocoded address, no group carries coordinates, or the client is already
/// in the nearest group, the skill changes nothing and reports why ("leave it"). Air-line (Haversine)
/// distance; real road routing (OpenRoute) is a separate follow-up.
/// </summary>
/// <param name="clientId">UUID of the client (customer or external employee) to place.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_client_to_nearest_group")]
public class AddClientToNearestGroupSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddClientToNearestGroupSkill(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdStr = GetRequiredString(parameters, "clientId");
        if (!Guid.TryParse(clientIdStr, out var clientId))
        {
            return SkillResult.Error($"Invalid client ID format: {clientIdStr}");
        }

        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return SkillResult.Error($"Client with ID {clientId} not found.");
        }

        var coordinate = client.Addresses
            .FirstOrDefault(a => !a.IsDeleted && a.Latitude.HasValue && a.Longitude.HasValue);
        if (coordinate == null)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, Added = false, Reason = "client has no geocoded address" },
                "This client has no address with coordinates, so the nearest group could not be determined. Nothing was changed.");
        }

        var groups = (await _groupRepository.List())
            .Where(g => !g.IsDeleted)
            .ToList();

        var anchors = groups
            .Where(g => g.Latitude.HasValue && g.Longitude.HasValue)
            .Select(g => new GroupAnchor(g.Id, g.Latitude!.Value, g.Longitude!.Value))
            .ToList();
        if (anchors.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, Added = false, Reason = "no group carries coordinates derived from its name" },
                "No group has a location resolved from its name (no geocoded groups), so the nearest group could not be determined. Nothing was changed.");
        }

        var nearest = CustomerGroupAssigner.FindNearest(
            new CustomerLocation(clientId, coordinate.Latitude!.Value, coordinate.Longitude!.Value),
            anchors);
        if (nearest == null)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, Added = false, Reason = "no nearest group could be determined" },
                "No nearest group could be determined for this client. Nothing was changed.");
        }

        var targetGroup = groups.First(g => g.Id == nearest.GroupId);

        var alreadyInTarget = client.GroupItems
            .Any(gi => !gi.IsDeleted && gi.GroupId == targetGroup.Id);
        if (alreadyInTarget)
        {
            return SkillResult.SuccessResult(
                new
                {
                    ClientId = clientId,
                    GroupId = targetGroup.Id,
                    GroupName = targetGroup.Name,
                    Added = false,
                    DistanceKm = Math.Round(nearest.DistanceKm, 2),
                    Reason = "client already in nearest group"
                },
                $"Client is already in the nearest group '{targetGroup.Name}'. Nothing was changed.");
        }

        var groupItem = new GroupItem
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            GroupId = targetGroup.Id,
            ValidFrom = DateTime.UtcNow,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        await _groupItemRepository.Add(groupItem);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                GroupItemId = groupItem.Id,
                ClientId = clientId,
                GroupId = targetGroup.Id,
                GroupName = targetGroup.Name,
                DistanceKm = Math.Round(nearest.DistanceKm, 2),
                Added = true
            },
            $"Client added to the nearest group '{targetGroup.Name}' ({Math.Round(nearest.DistanceKm, 2)} km air-line).");
    }
}
