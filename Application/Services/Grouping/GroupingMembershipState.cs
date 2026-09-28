// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Direct group memberships of clients and shifts during one analysis, including the virtual changes a
/// plan proposes. Scope(G) is the union of the direct members of G and of all its descendants; every
/// query reads the current state, so a proposal made earlier in the plan is already counted by later
/// steps (the spec's re-evaluation after adding). Scopes are cached per group and dropped on every
/// change of the corresponding memberships; a plan's tree only ever grows by a new root, which leaves
/// every existing subtree and therefore every cached scope unchanged.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingMembershipState
{
    private static readonly IReadOnlySet<Guid> Empty = new HashSet<Guid>();

    private readonly Dictionary<Guid, HashSet<Guid>> _groupsByClient = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _groupsByShift = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _clientsByGroup = new();
    private readonly Dictionary<Guid, HashSet<Guid>> _shiftsByGroup = new();
    private readonly Dictionary<Guid, IReadOnlySet<Guid>> _clientScopes = new();
    private readonly Dictionary<Guid, IReadOnlySet<Guid>> _shiftScopes = new();

    public static GroupingMembershipState From(
        IEnumerable<GroupingMembershipRecord> memberships,
        GroupingGroupTree tree,
        IReadOnlySet<Guid> knownClientIds,
        IReadOnlySet<Guid> knownShiftIds)
    {
        var state = new GroupingMembershipState();
        foreach (var membership in memberships.Where(m => tree.Contains(m.GroupId)))
        {
            if (membership.ClientId is Guid clientId && knownClientIds.Contains(clientId))
            {
                state.AddClient(clientId, membership.GroupId);
            }
            else if (membership.ShiftId is Guid shiftId && knownShiftIds.Contains(shiftId))
            {
                state.AddShift(shiftId, membership.GroupId);
            }
        }

        return state;
    }

    public void AddClient(Guid clientId, Guid groupId)
    {
        Link(_groupsByClient, clientId, groupId);
        Link(_clientsByGroup, groupId, clientId);
        _clientScopes.Clear();
    }

    public void AddShift(Guid shiftId, Guid groupId)
    {
        Link(_groupsByShift, shiftId, groupId);
        Link(_shiftsByGroup, groupId, shiftId);
        _shiftScopes.Clear();
    }

    public void RemoveClient(Guid clientId, Guid groupId)
    {
        _clientScopes.Clear();
        if (_groupsByClient.TryGetValue(clientId, out var groups))
        {
            groups.Remove(groupId);
        }

        if (_clientsByGroup.TryGetValue(groupId, out var clients))
        {
            clients.Remove(clientId);
        }
    }

    public IReadOnlySet<Guid> GroupsOfClient(Guid clientId) => _groupsByClient.GetValueOrDefault(clientId) ?? Empty;

    public IReadOnlySet<Guid> GroupsOfShift(Guid shiftId) => _groupsByShift.GetValueOrDefault(shiftId) ?? Empty;

    public bool IsPlanningUnit(Guid groupId) => _shiftsByGroup.TryGetValue(groupId, out var shifts) && shifts.Count > 0;

    public IReadOnlyList<Guid> PlanningUnits() =>
        _shiftsByGroup.Where(entry => entry.Value.Count > 0).Select(entry => entry.Key).ToList();

    public IReadOnlySet<Guid> ScopeClients(Guid groupId, GroupingGroupTree tree) =>
        Cached(_clientScopes, _clientsByGroup, groupId, tree);

    public IReadOnlySet<Guid> ScopeShifts(Guid groupId, GroupingGroupTree tree) =>
        Cached(_shiftScopes, _shiftsByGroup, groupId, tree);

    private static IReadOnlySet<Guid> Cached(
        Dictionary<Guid, IReadOnlySet<Guid>> cache, Dictionary<Guid, HashSet<Guid>> byGroup, Guid groupId, GroupingGroupTree tree)
    {
        if (!cache.TryGetValue(groupId, out var scope))
        {
            scope = Union(byGroup, groupId, tree);
            cache[groupId] = scope;
        }

        return scope;
    }

    private static IReadOnlySet<Guid> Union(Dictionary<Guid, HashSet<Guid>> byGroup, Guid groupId, GroupingGroupTree tree)
    {
        var result = new HashSet<Guid>();
        foreach (var member in tree.SelfAndDescendants(groupId))
        {
            if (byGroup.TryGetValue(member, out var items))
            {
                result.UnionWith(items);
            }
        }

        return result;
    }

    private static void Link(Dictionary<Guid, HashSet<Guid>> map, Guid key, Guid value)
    {
        if (!map.TryGetValue(key, out var set))
        {
            set = [];
            map[key] = set;
        }

        set.Add(value);
    }
}
