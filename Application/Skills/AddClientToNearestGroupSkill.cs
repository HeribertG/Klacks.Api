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
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Geo;

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AddClientToNearestGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "add_client_to_nearest_group";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ICompanyClock _companyClock;

    public AddClientToNearestGroupSkill(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IGroupItemRepository groupItemRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ICompanyClock companyClock)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _groupItemRepository = groupItemRepository;
        _selfApi = selfApi;
        _routes = routes;
        _companyClock = companyClock;
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

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(
            GetParameter<string>(parameters, "validFrom"), today);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessage);
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

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List())
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

        if (validFrom is null)
        {
            return SkillResult.Error(
                SkillDateParser.MissingStartDateMessage(
                    $"add this client to the nearest group '{targetGroup.Name}'"));
        }

        var groupItem = new GroupItem
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            GroupId = targetGroup.Id,
            ValidFrom = validFrom.Value,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        var resource = new GroupItemResource
        {
            ClientId = groupItem.ClientId,
            GroupId = groupItem.GroupId,
            ValidFrom = groupItem.ValidFrom,
            ValidUntil = groupItem.ValidUntil
        };

        var result = await _selfApi.PostAsync<GroupItemResource>(
            _routes.Resolve(typeof(GroupItemResource)), resource, context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

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
