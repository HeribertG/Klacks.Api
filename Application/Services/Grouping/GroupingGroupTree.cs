// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// In-memory group hierarchy of one analysis, resolved over Parent exactly like the plan view's recursive
/// parent walk (GetShiftSchedule.sql, GroupClientService.GetAllSubGroupIds) and deliberately not over the
/// nested set, whose Root/Lft/Rgt values are unreliable in real installations. A parent that is not
/// loaded (deleted) makes its child a root; visited sets make cycles harmless.
/// </summary>
/// <param name="groups">All non-deleted groups of the installation, plus virtual groups of a plan.</param>

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingGroupTree
{
    private readonly Dictionary<Guid, GroupingGroupRecord> _groups;
    private readonly Dictionary<Guid, List<Guid>> _children = new();
    private readonly Dictionary<Guid, IReadOnlySet<Guid>> _subtrees = new();
    private readonly Dictionary<Guid, IReadOnlyList<Guid>> _ancestors = new();

    public GroupingGroupTree(IEnumerable<GroupingGroupRecord> groups)
    {
        _groups = groups.GroupBy(group => group.Id).ToDictionary(group => group.Key, group => group.First());
        foreach (var group in _groups.Values)
        {
            if (group.ParentId is Guid parentId && _groups.ContainsKey(parentId))
            {
                if (!_children.TryGetValue(parentId, out var list))
                {
                    list = [];
                    _children[parentId] = list;
                }

                list.Add(group.Id);
            }
        }
    }

    public IReadOnlyCollection<Guid> GroupIds => _groups.Keys;

    public bool Contains(Guid groupId) => _groups.ContainsKey(groupId);

    public GroupingGroupRecord Get(Guid groupId) => _groups[groupId];

    public GroupingGroupTree WithGroup(GroupingGroupRecord group) => new(_groups.Values.Append(group));

    public IReadOnlySet<Guid> SelfAndDescendants(Guid groupId)
    {
        if (_subtrees.TryGetValue(groupId, out var cached))
        {
            return cached;
        }

        var result = new HashSet<Guid>();
        var pending = new Stack<Guid>();
        pending.Push(groupId);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (!result.Add(current))
            {
                continue;
            }

            if (_children.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    pending.Push(child);
                }
            }
        }

        _subtrees[groupId] = result;
        return result;
    }

    public IReadOnlyList<Guid> SelfAndAncestors(Guid groupId)
    {
        if (_ancestors.TryGetValue(groupId, out var cached))
        {
            return cached;
        }

        var chain = new List<Guid>();
        var seen = new HashSet<Guid>();
        Guid? current = groupId;
        while (current is Guid id && _groups.TryGetValue(id, out var group) && seen.Add(id))
        {
            chain.Add(id);
            current = group.ParentId;
        }

        _ancestors[groupId] = chain;
        return chain;
    }

    public int Depth(Guid groupId) => SelfAndAncestors(groupId).Count - 1;
}
