// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits one root group's subtree into holiday-calendar clusters: the root forms the first cluster, and every
/// descendant whose effective CalendarSelectionId differs from its parent's starts a cluster of its own (a
/// group without a selection inherits its parent's). Each cluster is scanned with its own holidays, so a
/// branch in another canton is not judged by the head office's calendar.
///
/// A shift belonging to groups of several clusters is owned by the most specific one: the cluster whose head
/// lies deepest in the tree (largest Lft). For groups in sibling clusters the same rule still yields exactly
/// one owner deterministically (larger Lft, then smaller head id) - a documented compromise, because two
/// calendars of equal depth have no natural winner.
/// </summary>
/// <param name="root">The root group of the subtree</param>
/// <param name="subtree">Every group of the root's tree, the root included</param>

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class CalendarClusterPartition
{
    private readonly Dictionary<Guid, CalendarCluster> _clusterByGroupId;

    private CalendarClusterPartition(IReadOnlyList<CalendarCluster> clusters, Dictionary<Guid, CalendarCluster> clusterByGroupId)
    {
        Clusters = clusters;
        _clusterByGroupId = clusterByGroupId;
    }

    public IReadOnlyList<CalendarCluster> Clusters { get; }

    public static CalendarClusterPartition Build(Group root, IReadOnlyCollection<Group> subtree)
    {
        var ordered = subtree
            .Where(group => group.Id != root.Id)
            .OrderBy(group => group.Lft)
            .ToList();

        var effective = new Dictionary<Guid, Guid?> { [root.Id] = root.CalendarSelectionId };
        var clusters = new List<CalendarCluster> { new(root, root.CalendarSelectionId) };

        foreach (var group in ordered)
        {
            var parentCalendar = group.Parent is Guid parentId && effective.TryGetValue(parentId, out var inherited)
                ? inherited
                : root.CalendarSelectionId;
            var ownCalendar = group.CalendarSelectionId ?? parentCalendar;
            effective[group.Id] = ownCalendar;

            if (ownCalendar != parentCalendar)
            {
                clusters.Add(new CalendarCluster(group, ownCalendar));
            }
        }

        var clusterByGroupId = new Dictionary<Guid, CalendarCluster>();
        foreach (var group in ordered.Prepend(root))
        {
            clusterByGroupId[group.Id] = clusters
                .Where(cluster => Contains(cluster.Head, group))
                .OrderByDescending(cluster => cluster.Head.Lft)
                .First();
        }

        return new CalendarClusterPartition(clusters, clusterByGroupId);
    }

    /// <summary>
    /// The cluster owning a shift that belongs to the given groups; null when none of them lies in this subtree.
    /// </summary>
    /// <param name="groupIds">Every group the shift belongs to</param>
    public CalendarCluster? OwnerOf(IEnumerable<Guid> groupIds) =>
        groupIds
            .Where(_clusterByGroupId.ContainsKey)
            .Select(groupId => _clusterByGroupId[groupId])
            .OrderByDescending(cluster => cluster.Head.Lft)
            .ThenBy(cluster => cluster.Head.Id)
            .FirstOrDefault();

    private static bool Contains(Group head, Group group) =>
        head.Id == group.Id || (head.Lft < group.Lft && head.Rgt > group.Rgt);
}
