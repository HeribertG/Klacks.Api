// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The model-facing facts of a seal_open_orders dry-run - the resolved group, how many matched orders are sealable and
/// blocked, the scope note of a group-restricted caller, what the group assignment would change and a capped sample of
/// the blocked orders with what they miss - shared by the skill's own apply=false preview and the confirmation preview
/// the autonomy gate shows before a held call, so both describe the same run in the same words.
/// </summary>

using Klacks.Api.Application.DTOs.Orders;

namespace Klacks.Api.Application.Skills;

internal static class SealOpenOrdersPreviewText
{
    private const int MaxBlockedPreviewNames = 10;

    private const string ScopedPreviewNote =
        "Only orders in the groups you can see are included; orders without a group can only be sealed by an " +
        "administrator. ";

    /// <param name="result">The dry-run result</param>
    /// <param name="scope">The scope the run was resolved with</param>
    /// <param name="groupNote">The note naming the resolved group, empty when none was named</param>
    public static string Describe(SealOpenOrdersResult result, OpenOrderSealingScope scope, string groupNote)
    {
        var scopeNote = scope.HasFullAccess ? string.Empty : ScopedPreviewNote;

        var autoAssignNote = result.AutoAssignRequested
            ? $"The group assignment would link {result.AutoAssignedCount} order(s) first, so up to " +
              $"{Math.Min(result.AutoAssignedCount, result.BlockedOnlyByMissingGroupCount)} of the blocked " +
              "order(s) would become sealable in the same call. "
            : result.BlockedOnlyByMissingGroupCount > 0
                ? $"{result.BlockedOnlyByMissingGroupCount} of them are blocked only by a missing group and " +
                  "would become sealable with autoAssignGroups=true. "
                : string.Empty;

        return result.TotalOrders == 0
            ? $"{groupNote}no open order matches the filter. {scopeNote}{autoAssignNote}"
            : $"{groupNote}{result.SealableCount} of {result.TotalOrders} open order(s) can be sealed as they " +
              $"are now, {result.BlockedCount} are blocked. {scopeNote}{autoAssignNote}{BlockedDetails(result)}";
    }

    public static string BlockedDetails(SealOpenOrdersResult result)
    {
        if (result.BlockedCount == 0)
        {
            return string.Empty;
        }

        var sample = string.Join("; ",
            result.BlockedSample.Take(MaxBlockedPreviewNames)
                .Select(b => $"'{b.OrderName}' missing {string.Join(", ", b.MissingRequirements)}"));
        var more = result.BlockedCount > MaxBlockedPreviewNames
            ? $" and {result.BlockedCount - MaxBlockedPreviewNames} more"
            : string.Empty;

        return $"Blocked: {sample}{more}. ";
    }
}
