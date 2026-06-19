// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Auto-resolves group coordinates. With a groupName it classifies and geocodes that one group
/// synchronously and returns the outcome; without one it queues every group that has no coordinates yet
/// for background resolution and reports how many were queued. Only groups whose name (with their parent
/// hierarchy) is confidently a real place get coordinates; everything else is left untouched.
/// </summary>
/// <param name="groupName">Optional. Resolve just this group now; omit to queue all groups without coordinates.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("geocode_location_groups")]
public class GeocodeLocationGroupsSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupLocationResolver _resolver;
    private readonly IGroupGeocodingQueue _queue;

    public GeocodeLocationGroupsSkill(
        IGroupRepository groupRepository,
        IGroupLocationResolver resolver,
        IGroupGeocodingQueue queue)
    {
        _groupRepository = groupRepository;
        _resolver = resolver;
        _queue = queue;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupName = GetParameter<string>(parameters, "groupName");
        var groups = (await _groupRepository.List()).Where(g => !g.IsDeleted).ToList();

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var group = groups.FirstOrDefault(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase));
            if (group == null)
            {
                return SkillResult.Error(
                    $"Group '{groupName}' not found. Use only existing group names — do not invent groups.");
            }

            var result = await _resolver.ResolveAsync(group.Id, cancellationToken);
            return SkillResult.SuccessResult(
                new { result.GroupId, result.GroupName, Outcome = result.Outcome.ToString(), result.Latitude, result.Longitude },
                $"Group '{result.GroupName}': {result.Outcome}" +
                (result.Latitude.HasValue ? $" ({result.Latitude:0.#####}, {result.Longitude:0.#####})." : "."));
        }

        var withoutCoordinates = groups
            .Where(g => !g.Latitude.HasValue || !g.Longitude.HasValue)
            .ToList();

        foreach (var group in withoutCoordinates)
        {
            _queue.Queue(group.Id);
        }

        return SkillResult.SuccessResult(
            new { QueuedCount = withoutCoordinates.Count },
            $"Queued {withoutCoordinates.Count} group(s) without coordinates for background geocoding. " +
            "Only those that are confidently a real place will receive coordinates; check back shortly.");
    }
}
