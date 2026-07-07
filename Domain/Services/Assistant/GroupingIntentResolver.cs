// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects a geographic grouping / group-assignment intent in a chat message and returns the real
/// grouping skills that must be guaranteed in the tool set for that turn. Without this, semantic skill
/// retrieval may fail to surface them (e.g. on a host where the embedding index is unavailable) and the
/// model — knowing it should group by address — invents a non-existent tool name instead of calling a
/// real one. Detection fires only when the message combines a grouping token with a location- or
/// assignment-signal, so read-only questions ("which groups are there?") do not trigger it. False
/// positives only add a few tools to the offered set, so the rule can be liberal.
/// </summary>
/// <param name="message">The current user chat message, matched case-insensitively.</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class GroupingIntentResolver
{
    private static readonly string[] GroupingTokens =
        ["gruppier", "gruppen", "gruppe", "group"];

    private static readonly string[] LocationOrAssignmentTokens =
        ["adresse", "address", "region", "kanton", "canton", "ort", "standort", "location",
         "nächst", "naechst", "nearest", "geograf", "geograph", "geographic",
         "zuordn", "zuteil", "zuweis", "assign", "verteil"];

    private static readonly string[] GuaranteedGroupingSkills =
        ["propose_employee_grouping", "apply_employee_grouping",
         "propose_customer_grouping", "apply_customer_grouping",
         "add_client_to_nearest_group", "list_groups"];

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var lower = message.ToLowerInvariant();

        var hasGrouping = GroupingTokens.Any(t => lower.Contains(t));
        var hasSignal = LocationOrAssignmentTokens.Any(t => lower.Contains(t))
            || (lower.Contains("ordne") && lower.Contains("zu"));

        return hasGrouping && hasSignal ? GuaranteedGroupingSkills : [];
    }
}
