// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets the geographic coordinates of a single group (by name). Coordinates are optional on a group —
/// only "location" groups (e.g. a city) carry them; qualification or hierarchy groups stay without.
/// Pass explicit latitude/longitude, or omit both to geocode the group from its name. The resolved
/// coordinates are returned so the user can confirm them before relying on the grouping.
/// </summary>
/// <param name="groupName">Name of the group whose location is set.</param>
/// <param name="latitude">Optional explicit latitude (degrees). When omitted the group name is geocoded.</param>
/// <param name="longitude">Optional explicit longitude (degrees). When omitted the group name is geocoded.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Domain.Constants;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class SetGroupLocationSkill : BaseSkillImplementation
{
    private const string SkillName = "set_group_location";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IGroupGeocoder _groupGeocoder;
    private readonly IKlacksSelfApiClient _selfApi;

    public SetGroupLocationSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IGroupGeocoder groupGeocoder,
        IKlacksSelfApiClient selfApi)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _groupGeocoder = groupGeocoder;
        _selfApi = selfApi;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetRequiredString(parameters, "groupName");
        var latitude = GetParameter<double?>(parameters, "latitude");
        var longitude = GetParameter<double?>(parameters, "longitude");

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());
        var active = groups.Where(g => !g.IsDeleted).ToList();
        var group = active.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
        string? ambiguityError = null;
        if (group == null)
        {
            group = FindUniqueByContains(active, groupName, out ambiguityError);
        }

        if (group == null)
        {
            if (ambiguityError != null)
            {
                return SkillResult.Error(ambiguityError);
            }

            var available = active.Count > 0
                ? "Available groups: " + string.Join(", ", active.Select(g => g.Name)) + "."
                : "There are no groups yet.";
            return SkillResult.Error(
                $"Group '{groupName}' not found. {available} Use only these real group names — do not invent groups.");
        }

        if (latitude.HasValue ^ longitude.HasValue)
        {
            return SkillResult.Error("Provide both latitude and longitude, or neither (to geocode by name).");
        }

        if (!latitude.HasValue)
        {
            var (geoLat, geoLon) = await _groupGeocoder.GeocodeAsync(group.Name, cancellationToken);
            if (!geoLat.HasValue || !geoLon.HasValue)
            {
                return SkillResult.Error(
                    $"Could not geocode '{group.Name}'. Provide explicit latitude and longitude instead.");
            }

            latitude = geoLat;
            longitude = geoLon;
        }

        var result = await _selfApi.PutAsync<object>(
            $"{SelfApiRoutes.Groups}/{group.Id}/location",
            new GroupLocationResource { Latitude = latitude!.Value, Longitude = longitude!.Value },
            context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new { GroupId = group.Id, group.Name, Latitude = latitude, Longitude = longitude },
            $"Location of group '{group.Name}' set to {latitude:0.#####}, {longitude:0.#####}.");
    }

    private static Group? FindUniqueByContains(IReadOnlyList<Group> groups, string groupName, out string? ambiguityError)
    {
        ambiguityError = null;
        var matches = groups
            .Where(g => g.Name.Contains(groupName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count > 1)
        {
            ambiguityError =
                $"'{groupName}' matches several groups: {string.Join(", ", matches.Select(g => g.Name))}. Be more specific.";
            return null;
        }

        return matches.Count == 1 ? matches[0] : null;
    }
}
