// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects a geographic grouping / group-assignment intent in a chat message and returns the real
/// grouping skills that must be guaranteed in the tool set for that turn. Without this, semantic skill
/// retrieval may fail to surface them (e.g. on a host where the embedding index is unavailable) and the
/// model — knowing it should group by address — invents a non-existent tool name instead of calling a
/// real one. Detection fires when the message combines a grouping token with a location- or
/// assignment-signal, OR a grouping token with a plain affirmation (e.g. "Ja, wende die Gruppierung an"
/// confirming the propose_* preview from the prior turn) — without the latter, the pure confirm turn
/// carries no location word and apply_grouping silently drops out of the tool set right when the model
/// needs to call it, so it explains its inability in free prose instead
/// (observed live: it also named the internal skill names while doing so). Read-only questions
/// ("which groups are there?") do not trigger it because AffirmationDetector refuses any message
/// containing a "?". A bare affirmation that does not repeat a grouping word (e.g. a plain "Ja" with no
/// "Gruppierung"/"group") still falls through to hasGrouping and is NOT covered by this guarantee.
/// False positives only add a few tools to the offered set, so the rule can be liberal.
/// The set also covers the two skills that give a group coordinates. Coordinates are not a precondition
/// of grouping — an exact city-name match wins — but they are the precondition of the nearest-group
/// fallback that applies when no group name matches the address city, and without those tools the model
/// can propose a grouping yet has no way to carry out the remedy it is told to offer. Both require
/// CanEditSettings and are dropped again by the permission filter for users without settings rights.
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
        ["propose_grouping", "apply_grouping",
         "add_client_to_nearest_group", "group_ungrouped_by_city_name", "list_groups",
         "geocode_location_groups", "set_group_location"];

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var lower = message.ToLowerInvariant();

        var hasGrouping = GroupingTokens.Any(t => lower.Contains(t));
        var hasSignal = LocationOrAssignmentTokens.Any(t => lower.Contains(t))
            || (lower.Contains("ordne") && lower.Contains("zu"))
            || AffirmationDetector.IsAffirmation(message);

        return hasGrouping && hasSignal ? GuaranteedGroupingSkills : [];
    }
}
