// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The (agent, shift, date) triples of a real-plan harmonizer run where the agent is not a member of the shift's group on
/// that day, so no swap hands the work to a person outside its group. A shift belongs to the groups its real-plan
/// GroupItem links name; an agent is a member when she holds a membership on that day (GroupMembershipWindowLoader and
/// GroupMembershipWindow.IsActiveOn: both bounds inclusive, temporary memberships of borrowed days included) in one of
/// those groups or in a group below one of them. A shift without a link to a live group stays unrestricted, and the
/// agent who holds the work on that day is never refused it.
/// </summary>
/// <param name="context">EF Core context holding GroupItem, Group and Membership</param>
/// <param name="agentIds">Agents of the run</param>
/// <param name="slots">The (shift, date) pairs of the plan a swap can move</param>
/// <param name="holders">The (agent, shift, date) triples the plan already holds</param>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftGroupIneligibilityQuery
{
    public static async Task<IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)>> LoadAsync(
        DataBaseContext context,
        IReadOnlyCollection<Guid> agentIds,
        IReadOnlyCollection<(Guid ShiftId, DateOnly Date)> slots,
        IReadOnlySet<(string AgentId, Guid ShiftId, DateOnly Date)> holders,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<(string, Guid, DateOnly)>();
        if (agentIds.Count == 0 || slots.Count == 0)
        {
            return result;
        }

        var shiftIds = slots.Select(s => s.ShiftId).Distinct().ToList();
        var links = await LinksQuery(context, shiftIds)
            .Select(gi => new { ShiftId = gi.ShiftId!.Value, gi.GroupId })
            .Distinct()
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return result;
        }

        var linkedGroupIds = links.Select(l => l.GroupId).Distinct().ToList();
        var linkedGroups = await context.Group
            .AsNoTracking()
            .Where(g => linkedGroupIds.Contains(g.Id))
            .Select(g => new { g.Id, Root = g.Root ?? g.Id, g.Lft, g.Rgt })
            .ToListAsync(cancellationToken);
        var roots = linkedGroups.Select(g => g.Root).Distinct().ToList();
        var treeGroups = await TreeQuery(context, roots)
            .Select(g => new { g.Id, Root = g.Root ?? g.Id, g.Lft, g.Rgt })
            .ToListAsync(cancellationToken);

        var subtreeOf = linkedGroups.ToDictionary(
            linked => linked.Id,
            linked => treeGroups
                .Where(g => g.Root == linked.Root && g.Lft >= linked.Lft && g.Rgt <= linked.Rgt)
                .Select(g => g.Id)
                .Append(linked.Id)
                .ToHashSet());
        var memberGroupsOfShift = links
            .Where(l => subtreeOf.ContainsKey(l.GroupId))
            .GroupBy(l => l.ShiftId)
            .ToDictionary(g => g.Key, g => g.SelectMany(l => subtreeOf[l.GroupId]).ToHashSet());
        if (memberGroupsOfShift.Count == 0)
        {
            return result;
        }

        var allGroupIds = memberGroupsOfShift.Values.SelectMany(ids => ids).Distinct().ToList();
        var from = slots.Min(s => s.Date);
        var until = slots.Max(s => s.Date);
        var windows = (await GroupMembershipWindowLoader.LoadAsync(context, allGroupIds, agentIds, from, until, cancellationToken))
            .GroupBy(entry => entry.Window.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (shiftId, date) in slots.Distinct())
        {
            if (!memberGroupsOfShift.TryGetValue(shiftId, out var memberGroups))
            {
                continue;
            }

            foreach (var agentId in agentIds)
            {
                var triple = (agentId.ToString(), shiftId, date);
                if (holders.Contains(triple))
                {
                    continue;
                }

                var isMember = windows.TryGetValue(agentId, out var agentWindows)
                    && agentWindows.Any(entry => memberGroups.Contains(entry.GroupId) && entry.Window.IsActiveOn(date));
                if (!isMember)
                {
                    result.Add(triple);
                }
            }
        }

        return result;
    }

    /// <summary>The real-plan links of the shifts to their groups.</summary>
    internal static IQueryable<GroupItem> LinksQuery(DataBaseContext context, IReadOnlyCollection<Guid> shiftIds)
        => context.GroupItem
            .AsNoTracking()
            .Where(gi => gi.ShiftId != null
                && shiftIds.Contains(gi.ShiftId.Value)
                && !gi.IsDeleted
                && gi.AnalyseToken == null
                && gi.ScenarioSourceGroupItemId == null);

    /// <summary>Every group of the trees with the given roots (a root group carries its own id or none as root).</summary>
    internal static IQueryable<Group> TreeQuery(DataBaseContext context, IReadOnlyCollection<Guid> roots)
        => context.Group
            .AsNoTracking()
            .Where(g => roots.Contains(g.Root ?? g.Id));
}
