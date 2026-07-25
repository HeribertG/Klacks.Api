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
/// Core languages (de/en/fr/it) are handled by hardcoded stems. Plugin language keywords are loaded at
/// startup via Configure() from grouping-intent.json files in each language plugin, mirroring
/// MutationIntentDetector's plugin extension.
/// The set also covers the skills that give a group coordinates (geocode_location_groups,
/// set_group_location) plus the read-only status check for the async geocoding queue
/// (check_group_geocoding_status). Coordinates are not a precondition of grouping — an exact city-name
/// match wins — but they are the precondition of the nearest-group fallback that applies when no group
/// name matches the address city, and without those tools the model can propose a grouping yet has no
/// way to carry out the remedy it is told to offer, or to tell whether that remedy is still in progress.
/// geocode_location_groups and set_group_location require CanEditSettings and are dropped again by the
/// permission filter for users without settings rights; check_group_geocoding_status only needs
/// CanViewGroups.
/// </summary>
/// <param name="message">The current user chat message, matched case-insensitively.</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class GroupingIntentResolver
{
    private static readonly string[] GroupingTokens =
        ["gruppier", "gruppen", "gruppe", "group", "gruppo"];

    private static readonly string[] LocationOrAssignmentTokens =
        ["adresse", "address", "region", "kanton", "canton", "ort", "standort", "location",
         "nächst", "naechst", "nearest", "geograf", "geograph", "geographic",
         "zuordn", "zuteil", "zuweis", "assign", "verteil"];

    private static readonly string[] GuaranteedGroupingSkills =
        ["propose_grouping", "apply_grouping",
         "add_client_to_nearest_group", "group_ungrouped_by_city_name", "list_groups",
         "geocode_location_groups", "set_group_location", "check_group_geocoding_status"];

    private static readonly object _configureLock = new();
    private static string[] _pluginGroupingTokens = [];
    private static string[] _pluginLocationOrAssignmentTokens = [];

    /// <summary>
    /// Extends detection with plugin language keywords. Called once at startup by
    /// GroupingIntentPluginLoader after reading grouping-intent.json from each language plugin.
    /// </summary>
    public static void Configure(IEnumerable<string> groupingTokens, IEnumerable<string> locationOrAssignmentTokens)
    {
        lock (_configureLock)
        {
            _pluginGroupingTokens = PluginPhraseMatcher.Merge(_pluginGroupingTokens, groupingTokens);
            _pluginLocationOrAssignmentTokens = PluginPhraseMatcher.Merge(_pluginLocationOrAssignmentTokens, locationOrAssignmentTokens);
        }
    }

    public static IReadOnlyList<string> GuaranteedSkillNames(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return [];
        }

        var lower = message.ToLowerInvariant();

        var hasGrouping = GroupingTokens.Any(t => lower.Contains(t))
            || _pluginGroupingTokens.Any(t => lower.Contains(t));
        var hasSignal = LocationOrAssignmentTokens.Any(t => lower.Contains(t))
            || _pluginLocationOrAssignmentTokens.Any(t => lower.Contains(t))
            || (lower.Contains("ordne") && lower.Contains("zu"))
            || AffirmationDetector.IsAffirmation(message);

        return hasGrouping && hasSignal ? GuaranteedGroupingSkills : [];
    }
}
