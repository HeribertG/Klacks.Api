// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Carries lowered group starts up the Parent chain: every ancestor of a lowered group starts with the earliest of its own
/// start and the lowered starts of its descendants, so a parent never begins after one of its children. Pure and
/// side-effect free; a start is never raised and groups that already start early enough, or are not part of the tree
/// handed in, are not returned.
/// </summary>
/// <param name="groups">The groups of the affected trees with their parent and current start</param>
/// <param name="newStarts">The lowered start per target group</param>

namespace Klacks.Api.Domain.Services.Groups;

public static class GroupValidFromPropagation
{
    public static Dictionary<Guid, DateTime> Resolve(
        IReadOnlyCollection<(Guid Id, Guid? Parent, DateTime ValidFrom)> groups,
        IReadOnlyDictionary<Guid, DateTime> newStarts)
    {
        var byId = groups.ToDictionary(group => group.Id);
        var lowest = new Dictionary<Guid, DateTime>();

        foreach (var (groupId, start) in newStarts)
        {
            Guid? node = groupId;
            var guard = byId.Count + 1;
            while (node is { } id && byId.ContainsKey(id) && guard-- > 0)
            {
                lowest[id] = lowest.TryGetValue(id, out var known) && known <= start ? known : start;
                node = byId[id].Parent;
            }
        }

        return lowest
            .Where(entry => entry.Value < byId[entry.Key].ValidFrom)
            .ToDictionary(entry => entry.Key, entry => entry.Value);
    }
}
