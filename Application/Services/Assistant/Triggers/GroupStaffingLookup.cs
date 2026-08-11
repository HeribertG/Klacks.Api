// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers whether a group carries any staffing at all — either its own clients/shifts or those of
/// a descendant group. Period triggers use it to stay silent about empty groups: a period of a
/// group nobody works in has nothing to close, so a reminder about it is pure noise.
/// </summary>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class GroupStaffingLookup
{
    private readonly HashSet<Guid> _staffedGroupIds;

    private GroupStaffingLookup(HashSet<Guid> staffedGroupIds)
    {
        _staffedGroupIds = staffedGroupIds;
    }

    /// <summary>
    /// Marks every group holding clients or shifts plus all of its ancestors. The tree is a nested
    /// set with several roots, so an ancestor is only an ancestor within the same Root.
    /// </summary>
    /// <param name="groups">All groups of the tenant.</param>
    /// <param name="groupIdsWithMembers">Ids of groups that directly hold clients or shifts.</param>
    public static GroupStaffingLookup Build(
        IReadOnlyCollection<Group> groups,
        IReadOnlyCollection<Guid> groupIdsWithMembers)
    {
        var directlyStaffed = groups.Where(group => groupIdsWithMembers.Contains(group.Id)).ToList();
        var staffed = directlyStaffed.Select(group => group.Id).ToHashSet();

        foreach (var group in groups)
        {
            if (staffed.Contains(group.Id))
            {
                continue;
            }

            if (directlyStaffed.Any(descendant => IsAncestorOf(group, descendant)))
            {
                staffed.Add(group.Id);
            }
        }

        return new GroupStaffingLookup(staffed);
    }

    public bool IsStaffed(Guid groupId) => _staffedGroupIds.Contains(groupId);

    private static bool IsAncestorOf(Group candidate, Group descendant)
    {
        return candidate.Root == descendant.Root
            && candidate.Lft < descendant.Lft
            && candidate.Rgt > descendant.Rgt;
    }
}
