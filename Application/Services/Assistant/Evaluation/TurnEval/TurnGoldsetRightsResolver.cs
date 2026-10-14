// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the rights list an item is replayed with. An item without a role keeps the rights the run was
/// started with, unchanged and in the same list instance, so every existing goldset replays bit-identically.
/// An item with a role gets the rights Permissions.ExpandRoles produces for that tier - the same expansion
/// the chat endpoint applies to a real caller - so no second rights truth exists in the eval.
/// </summary>
/// <param name="item">The goldset item to be replayed</param>
/// <param name="runRights">The rights the whole run was started with</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnGoldsetRightsResolver
{
    public static List<string> Resolve(TurnGoldsetItem item, List<string> runRights) =>
        item.Role switch
        {
            null => runRights,
            TurnGoldsetRole.Admin => Permissions.ExpandRoles([Roles.Admin]),
            TurnGoldsetRole.Supervisor => Permissions.ExpandRoles([Roles.Authorised]),
            TurnGoldsetRole.Planner => Permissions.ExpandRoles([]),
            _ => throw new ArgumentOutOfRangeException(nameof(item), item.Role, "Unknown goldset role.")
        };
}
