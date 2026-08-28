// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Weekly report of what the learning loop picked up: how many phrasings and capabilities it learned and
/// how many wishes it still cannot serve. An administrator concern, not a scheduling gap, so it reaches
/// admins only. Severity is medium on purpose - the digest must appear as an inbox line and a badge, and
/// must never interrupt a conversation with a chat bubble.
/// </summary>
/// <param name="WeekStartUtc">Monday of the reported week, the window the counters were taken from</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record KlacksyLearnedDigestTriggerEvent(
    DateOnly WeekStartUtc,
    int Phrases,
    int Capabilities,
    int Unfulfillable) : IAgentTriggerEvent
{
    public int Total => Phrases + Capabilities + Unfulfillable;

    public string Kind => AgentTriggerKinds.KlacksyLearnedDigest;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool AdminOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.KlacksyLearnedDigest;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["phrases"] = Phrases.ToString(CultureInfo.InvariantCulture),
        ["capabilities"] = Capabilities.ToString(CultureInfo.InvariantCulture),
        ["unfulfillable"] = Unfulfillable.ToString(CultureInfo.InvariantCulture),
        ["total"] = Total.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(WeekStartUtc);

    public string? ActionRoute => ProactiveActionRoutes.Settings;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Target] = ProactiveActionRoutes.SettingsTargetKlacksyLearning
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["weekStart"] = WeekStartUtc,
        ["phrases"] = Phrases,
        ["capabilities"] = Capabilities,
        ["unfulfillable"] = Unfulfillable,
        ["total"] = Total
    };

    /// <summary>
    /// ISO week of the digest, so at most one digest per calendar week reaches an administrator no matter
    /// how often the hourly detector tick runs.
    /// </summary>
    /// <param name="weekStartUtc">Any date inside the week the digest is dispatched in</param>
    public static string DedupKeyFor(DateOnly weekStartUtc) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{ISOWeek.GetYear(weekStartUtc.ToDateTime(TimeOnly.MinValue))}-W{ISOWeek.GetWeekOfYear(weekStartUtc.ToDateTime(TimeOnly.MinValue)):D2}");
}
