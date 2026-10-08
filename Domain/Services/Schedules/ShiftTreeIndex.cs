// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Lookup tables over the tree rows of order families, used by <see cref="ShiftScopeExpander"/>.
/// </summary>
/// <param name="ById">Row per shift id</param>
/// <param name="ByOrder">Shift ids per OriginalId (the order they belong to)</param>
/// <param name="ChildrenOf">Shift ids per ParentId (the piece they were cut from)</param>
internal sealed record ShiftTreeIndex(
    IReadOnlyDictionary<Guid, ShiftTreeRow> ById,
    IReadOnlyDictionary<Guid, List<Guid>> ByOrder,
    IReadOnlyDictionary<Guid, List<Guid>> ChildrenOf)
{
    public static ShiftTreeIndex Build(IReadOnlyCollection<ShiftTreeRow> rows)
    {
        var byId = new Dictionary<Guid, ShiftTreeRow>();
        var byOrder = new Dictionary<Guid, List<Guid>>();
        var childrenOf = new Dictionary<Guid, List<Guid>>();
        foreach (var row in rows)
        {
            byId[row.Id] = row;
            if (row.OriginalId is { } orderId)
            {
                Add(byOrder, orderId, row.Id);
            }

            if (row.ParentId is { } parentId)
            {
                Add(childrenOf, parentId, row.Id);
            }
        }

        return new ShiftTreeIndex(byId, byOrder, childrenOf);
    }

    private static void Add(Dictionary<Guid, List<Guid>> map, Guid key, Guid value)
    {
        if (!map.TryGetValue(key, out var list))
        {
            list = [];
            map[key] = list;
        }

        list.Add(value);
    }
}