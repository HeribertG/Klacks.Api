// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the group filter of the skills that work on the services of a group (bundle_nearby_timerange_shifts_into_container,
/// build_weekly_tours, split_shifts_by_weekday) from a group name and/or group ids.
/// The name goes through the shared GroupResolver under the strict policy (an exact name wins, an ambiguous name lists
/// its candidates, an unknown name lists the available groups) over the groups the caller may see, so a hidden group is
/// answered like a missing one. Every id must be a GUID of a visible group; a value that is no GUID is refused instead
/// of being skipped, because skipping it silently dropped the filter. A requested group without any directly linked
/// service is refused as well: the available-task list would otherwise fall back to the services of all groups.
/// </summary>
/// <param name="groupRepository">Lists and reads the groups</param>
/// <param name="groupVisibilityGuard">Limits the groups to the caller's visibility scope</param>
/// <param name="groupItemRepository">Tells whether a group has any directly linked service</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

public sealed class BundleGroupFilterResolver
{
    private const char GroupIdSeparator = ',';
    private const string GroupNameSeparator = "', '";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IGroupItemRepository _groupItemRepository;

    public BundleGroupFilterResolver(
        IGroupRepository groupRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGroupItemRepository groupItemRepository)
    {
        _groupRepository = groupRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _groupItemRepository = groupItemRepository;
    }

    /// <param name="groupIdsRaw">Comma-separated group GUIDs, or null</param>
    /// <param name="groupName">Group name as the user said it, or null</param>
    public async Task<(List<Group>? Groups, string? Error)> ResolveAsync(
        string? groupIdsRaw, string? groupName, CancellationToken cancellationToken)
    {
        var groups = new List<Group>();

        var (groupsById, idError) = await ResolveIdsAsync(groupIdsRaw, cancellationToken);
        if (groupsById is null)
        {
            return (null, idError);
        }

        groups.AddRange(groupsById);

        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var (named, nameError) = await ResolveNameAsync(groupName, cancellationToken);
            if (named is null)
            {
                return (null, nameError);
            }

            groups.Add(named);
        }

        var distinct = groups.GroupBy(g => g.Id).Select(g => g.First()).ToList();
        if (distinct.Count == 0)
        {
            return (distinct, null);
        }

        var linkedShiftIds = await _groupItemRepository.GetShiftIdsByGroupIds(
            distinct.Select(g => g.Id).ToList(), cancellationToken);
        if (linkedShiftIds.Count == 0)
        {
            return (null,
                $"The group '{string.Join(GroupNameSeparator, distinct.Select(g => g.Name))}' has no services linked " +
                "directly to it (services of its subgroups do not count), so there is nothing to process — no " +
                "container was created and nothing was changed. Ask the user which group holds the services.");
        }

        return (distinct, null);
    }

    private async Task<(List<Group>? Groups, string? Error)> ResolveIdsAsync(
        string? groupIdsRaw, CancellationToken cancellationToken)
    {
        var groups = new List<Group>();
        if (string.IsNullOrWhiteSpace(groupIdsRaw))
        {
            return (groups, null);
        }

        var entries = groupIdsRaw.Split(GroupIdSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in entries)
        {
            if (!Guid.TryParse(entry, out var groupId))
            {
                return (null,
                    $"'{entry}' is not a group id — groupIds only accepts group UUIDs. Pass the group's name as " +
                    "groupName instead.");
            }

            var group = await _groupRepository.Get(groupId);
            if (group == null || !await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
            {
                return (null, $"Group with ID {groupId} not found.");
            }

            groups.Add(group);
        }

        return (groups, null);
    }

    private async Task<(Group? Group, string? Error)> ResolveNameAsync(string groupName, CancellationToken cancellationToken)
    {
        var scope = await _groupVisibilityGuard.GetScopeAsync(cancellationToken);
        var visibleIds = scope.VisibleGroupIds.ToHashSet();
        var visibleGroups = (await _groupRepository.List())
            .Where(g => scope.IsUnrestricted || visibleIds.Contains(g.Id))
            .ToList();

        return GroupResolver.Resolve(visibleGroups, groupName);
    }
}
