// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Hands what is set on a shift down to the pieces the planner actually staffs. Something set on an order
/// (original order, sealed order or its plannable copy) reaches every shift of that order: the shifts whose
/// OriginalId is the order (OriginalId ?? Id of the source) and their cut descendants. Something set on a cut
/// piece reaches that piece and its descendants along ParentId. Applied to shift preferences (an explicit entry
/// on a shift beats an inherited one; conflicting inherited entries resolve to Blacklist) and to required
/// qualifications (a piece needs its own requirements plus those of every source above it).
/// </summary>
public static class ShiftScopeExpander
{
    /// <summary>
    /// Every shift that receives what is set on <paramref name="sourceId"/>, the source itself included.
    /// </summary>
    /// <param name="sourceId">Shift the attribute is set on</param>
    /// <param name="rows">Tree rows of the source's order family</param>
    public static IReadOnlySet<Guid> ReceiversOf(Guid sourceId, IReadOnlyCollection<ShiftTreeRow> rows)
        => ReceiversOf(sourceId, ShiftTreeIndex.Build(rows));

    /// <summary>
    /// Explicit preferences plus the ones each shift inherits from a preference on an ancestor.
    /// </summary>
    /// <param name="explicitPreferences">Preferences as stored</param>
    /// <param name="rows">Tree rows of the order families the preferred shifts belong to</param>
    public static IReadOnlyList<ScopedShiftPreference> ExpandPreferences(
        IReadOnlyCollection<ScopedShiftPreference> explicitPreferences,
        IReadOnlyCollection<ShiftTreeRow> rows)
    {
        var index = ShiftTreeIndex.Build(rows);
        var explicitKeys = explicitPreferences.Select(p => (p.ClientId, p.ShiftId)).ToHashSet();
        var inherited = new Dictionary<(Guid ClientId, Guid ShiftId), ShiftPreferenceType>();

        foreach (var preference in explicitPreferences)
        {
            foreach (var receiver in ReceiversOf(preference.ShiftId, index))
            {
                var key = (preference.ClientId, receiver);
                if (receiver == preference.ShiftId || explicitKeys.Contains(key))
                {
                    continue;
                }

                inherited[key] = inherited.TryGetValue(key, out var known) && known == ShiftPreferenceType.Blacklist
                    ? ShiftPreferenceType.Blacklist
                    : preference.PreferenceType;
            }
        }

        return explicitPreferences
            .Concat(inherited
                .OrderBy(entry => entry.Key.ClientId)
                .ThenBy(entry => entry.Key.ShiftId)
                .Select(entry => new ScopedShiftPreference(entry.Key.ClientId, entry.Key.ShiftId, entry.Value)))
            .ToList();
    }

    /// <summary>
    /// For each receiver the other shifts whose attributes it inherits (its order and its cut ancestors).
    /// </summary>
    /// <param name="receiverIds">Shifts that are staffed</param>
    /// <param name="rows">Tree rows of their order families</param>
    public static IReadOnlyDictionary<Guid, IReadOnlySet<Guid>> InheritanceSourcesOf(
        IReadOnlyCollection<Guid> receiverIds,
        IReadOnlyCollection<ShiftTreeRow> rows)
    {
        var index = ShiftTreeIndex.Build(rows);
        var wanted = receiverIds.ToHashSet();
        var sources = new Dictionary<Guid, HashSet<Guid>>();

        foreach (var source in rows)
        {
            foreach (var receiver in ReceiversOf(source.Id, index))
            {
                if (receiver == source.Id || !wanted.Contains(receiver))
                {
                    continue;
                }

                if (!sources.TryGetValue(receiver, out var set))
                {
                    set = [];
                    sources[receiver] = set;
                }

                set.Add(source.Id);
            }
        }

        return sources.ToDictionary(entry => entry.Key, entry => (IReadOnlySet<Guid>)entry.Value);
    }

    private static IReadOnlySet<Guid> ReceiversOf(Guid sourceId, ShiftTreeIndex index)
    {
        var receivers = new HashSet<Guid> { sourceId };
        if (!index.ById.TryGetValue(sourceId, out var source))
        {
            return receivers;
        }

        if (source.Status != ShiftStatus.SplitShift)
        {
            var orderId = source.OriginalId ?? source.Id;
            receivers.UnionWith(index.ByOrder.GetValueOrDefault(orderId, []));
        }

        var pending = new Queue<Guid>(receivers);
        while (pending.Count > 0)
        {
            foreach (var child in index.ChildrenOf.GetValueOrDefault(pending.Dequeue(), []))
            {
                if (receivers.Add(child))
                {
                    pending.Enqueue(child);
                }
            }
        }

        return receivers;
    }
}
