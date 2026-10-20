// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Start date of every group a partition run creates: the earliest start among the persons placed in the group or in any
/// group below it (in Fixed mode the common start for all), so a new group never begins after its first member and never
/// later than today, and a parent starts with the earliest of its children.
/// </summary>
/// <param name="plan">The partition plan with its groups (key, parent key) and the person-to-leaf assignments</param>
/// <param name="startPlan">The start dates of the persons</param>

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public static class PartitionGroupStarts
{
    public static Dictionary<string, DateTime> Resolve(GroupPartitionPlan plan, GroupMembershipStartPlan startPlan)
    {
        var parentByKey = plan.Groups.ToDictionary(group => group.Key, group => group.ParentKey, StringComparer.Ordinal);
        var membersByKey = new Dictionary<string, List<Guid>>(StringComparer.Ordinal);

        foreach (var assignment in plan.Assignments)
        {
            string? key = assignment.LeafGroupKey;
            while (key is not null)
            {
                if (!membersByKey.TryGetValue(key, out var members))
                {
                    members = [];
                    membersByKey[key] = members;
                }

                members.Add(assignment.ClientId);
                key = parentByKey.GetValueOrDefault(key);
            }
        }

        return plan.Groups.ToDictionary(
            group => group.Key,
            group => startPlan.GroupStart(membersByKey.GetValueOrDefault(group.Key) ?? []),
            StringComparer.Ordinal);
    }
}
