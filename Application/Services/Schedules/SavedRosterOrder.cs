// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies a user's saved individual roster order (ClientSortPreference) to the employees of a wizard run. The ranked
/// employees come first by SortOrder; the unranked ones follow in their incoming order. When no employee of the run is
/// ranked (no saved order, or one that only names other people), the incoming order is returned unchanged and
/// UserDefined is false, so the wizard falls back to its automatic order.
/// </summary>
/// <param name="agentIds">Employees of the run in their incoming order</param>
/// <param name="savedOrder">The user's saved order for the group</param>

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Schedules;

public static class SavedRosterOrder
{
    public static (IReadOnlyList<Guid> AgentIds, bool UserDefined) Apply(
        IReadOnlyList<Guid> agentIds,
        IReadOnlyList<ClientSortPreference> savedOrder)
    {
        var rankByClient = new Dictionary<Guid, int>();
        foreach (var preference in savedOrder.OrderBy(preference => preference.SortOrder))
        {
            rankByClient.TryAdd(preference.ClientId, preference.SortOrder);
        }

        var ranked = agentIds
            .Where(rankByClient.ContainsKey)
            .OrderBy(agentId => rankByClient[agentId])
            .ToList();
        if (ranked.Count == 0)
        {
            return (agentIds, false);
        }

        var unranked = agentIds.Where(agentId => !rankByClient.ContainsKey(agentId));
        return (ranked.Concat(unranked).ToList(), true);
    }
}
