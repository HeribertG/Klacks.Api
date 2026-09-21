// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the content parameters of a proactive message from the CURRENT payload of the condition-ledger
/// row it reports, falling back to the parameters frozen onto the dispatch row at first delivery. Shared
/// by the reminder sweep and the inbox read, which is the whole point: an aggregated finding states counts
/// and names in its sentence, those move while the finding stays open, and two copies of this merge would
/// let the reminder and the inbox report different numbers for the same row - the inbox being the one that
/// still showed the count of the day the finding was first detected.
///
/// The live values are MERGED over the frozen ones instead of replacing them, because the two sets are not
/// the same shape: the payload is what the detector captured (a period label, a count, the affected rows),
/// the frozen parameters are what the message's i18n string interpolates. Replacing would silently drop
/// every placeholder the payload happens not to carry and render the sentence with holes in it. The worst
/// case of the merge is therefore exactly the frozen behaviour.
///
/// Only CONTENT parameters are resolved this way. Action parameters stay the dispatch row's own: they
/// address the route the user lands on, and a payload key that happened to share their name would silently
/// redirect the click.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ProactiveContentParamMerge
{
    /// <summary>
    /// The content parameters to render with: every scalar entry of the live payload written over the
    /// frozen parameters, and the frozen ones alone when the payload carries nothing usable.
    /// </summary>
    /// <param name="frozenParams">Parameters serialized onto the dispatch row at first delivery, or null when it carries none.</param>
    /// <param name="livePayloadJson">The ledger row's current PayloadJson; null, blank or unreadable leaves the frozen parameters in place.</param>
    /// <param name="payloadError">
    /// The parse failure when the payload was present but not valid JSON, so a caller on a background
    /// path can log it. Null both when the payload parsed and when there was none to parse - the two are
    /// told apart because only the first is worth a log line.
    /// </param>
    public static IReadOnlyDictionary<string, string>? MergeLiveOverFrozen(
        IReadOnlyDictionary<string, string>? frozenParams,
        string? livePayloadJson,
        out JsonException? payloadError)
    {
        var liveParams = ParseLiveScalars(livePayloadJson, out payloadError);

        if (liveParams == null || liveParams.Count == 0)
        {
            return frozenParams;
        }

        if (frozenParams == null || frozenParams.Count == 0)
        {
            return liveParams;
        }

        var merged = new Dictionary<string, string>(frozenParams, StringComparer.Ordinal);
        foreach (var liveParam in liveParams)
        {
            merged[liveParam.Key] = liveParam.Value;
        }

        return merged;
    }

    /// <summary>
    /// The scalar entries of a condition payload, as interpolation values. The payload is free-form per
    /// detector kind and routinely nests objects and arrays; those are skipped rather than stringified,
    /// because their raw JSON in a user-facing sentence is noise, not information. An unreadable payload
    /// degrades to no live values at all, which leaves the frozen ones in place.
    /// </summary>
    /// <param name="payloadJson">The ledger row's current PayloadJson.</param>
    /// <param name="payloadError">The parse failure, or null when there was none.</param>
    private static Dictionary<string, string>? ParseLiveScalars(string? payloadJson, out JsonException? payloadError)
    {
        payloadError = null;

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadJson);
            if (payload == null)
            {
                return null;
            }

            var scalars = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in payload)
            {
                if (TryRenderScalar(entry.Value, out var rendered))
                {
                    scalars[entry.Key] = rendered;
                }
            }

            return scalars;
        }
        catch (JsonException ex)
        {
            payloadError = ex;
            return null;
        }
    }

    /// <summary>
    /// A payload value as text, for the value kinds a message can interpolate. Numbers are taken from
    /// their raw JSON text, which is culture-invariant by definition of the format.
    ///
    /// Capped at ProactiveTriggerDispatchLimits.ContentParamValueMaxLength, the same cap
    /// AgentTriggerService applies to the frozen parameters. Without it the merge would hand a longer
    /// value back than the row could ever have stored: the two sides of a collision are not always the
    /// same length - OrderImportFailedTriggerEvent carries the identical exception message in both its
    /// summary parameter (capped) and its payload (verbatim) - and the payload column has no width limit
    /// of its own, so an uncapped live value puts an unbounded string into a user-facing sentence and
    /// into every poll of the inbox list.
    /// </summary>
    /// <param name="element">One payload entry's value.</param>
    /// <param name="rendered">The text to interpolate, or the empty string when the value kind carries none.</param>
    private static bool TryRenderScalar(JsonElement element, out string rendered)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                rendered = Cap(element.GetString());
                return true;
            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                rendered = Cap(element.GetRawText());
                return true;
            default:
                rendered = string.Empty;
                return false;
        }
    }

    private static string Cap(string? value) =>
        ProactiveTextTruncator.Cap(value, ProactiveTriggerDispatchLimits.ContentParamValueMaxLength)
            ?? string.Empty;
}
