// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the compact system-context block listing open condition-ledger findings relevant to the
/// user's current page (Etappe 3g of the Klacksy-proactive plan), so Klacksy can speak about them without
/// the user asking first. Returns an empty string when there is nothing to show, so callers can omit the
/// block. Every line carries LastSeenAtUtc and is phrased as a past observation ("was last observed at
/// ..."), never as a guaranteed-current fact: a detector re-confirms a condition once per tick, so a row
/// can be stale by up to one tick interval, and this block has no cheap way to tell "just detected" apart
/// from "not reconfirmed in a while" other than the timestamp itself.
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class AgentConditionContextRenderer
{
    private const string Header =
        "[OPEN_FINDINGS] Conditions your background detectors are tracking near the user's current context, " +
        "most urgent first. Each was last observed at the given UTC time by an automated check, not just now - " +
        "mention them as past observations (\"was last seen ...\"), never as freshly reconfirmed facts:";

    public static string Render(IReadOnlyList<AgentCondition>? conditions)
    {
        if (conditions is null || conditions.Count == 0)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine(Header);

        foreach (var condition in conditions)
        {
            sb.AppendLine($"- {FormatLine(condition)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string FormatLine(AgentCondition condition)
    {
        var severity = condition.Severity.ToUpperInvariant();
        var refs = FormatReferences(condition);
        var lastSeen = condition.LastSeenAtUtc.ToString("O", CultureInfo.InvariantCulture);
        return $"[{severity}] {condition.TriggerKind}{refs} — last observed {lastSeen}";
    }

    private static string FormatReferences(AgentCondition condition)
    {
        var parts = new List<string>();
        if (condition.EntityId.HasValue)
        {
            parts.Add($"entity {condition.EntityId.Value}");
        }

        if (condition.GroupId.HasValue)
        {
            parts.Add($"group {condition.GroupId.Value}");
        }

        return parts.Count == 0 ? string.Empty : $" ({string.Join(", ", parts)})";
    }
}
