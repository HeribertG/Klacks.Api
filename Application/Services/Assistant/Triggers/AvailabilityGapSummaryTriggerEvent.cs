// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired once per scan when at least one plannable employee has entered no availability for the
/// upcoming calendar month. Deliberately ONE event for the whole workforce rather than one per
/// employee: the per-employee shape produced one inbox row per employee per recipient, which is a
/// mailing and not a notification.
///
/// The dedup key is the scanned MONTH alone - deliberately not the company day. The ledger row of a
/// finding must leave the active fingerprint set when the finding is FIXED; a day-stamped key would
/// make it leave at midnight regardless, so reconcile would resolve it and the partial unique index
/// would re-arm a fresh row the next morning. That turns a state-based ledger row into a timer and
/// re-notifies every planner every single day. Keeping the month means the aggregate fires once per
/// period and falls silent again as soon as the availabilities are entered.
///
/// The sentence is its own i18n key (AvailabilityGapSummary), never the per-employee one: that
/// sentence names a single person, so filling it with a name list would state something untrue about
/// everybody else it names.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record AvailabilityGapSummaryTriggerEvent(
    IReadOnlyList<ProactiveAffectedClient> AffectedClients,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int DaysUntilPeriodStart) : IAgentTriggerEvent
{
    private const int HighSeverityLeadDays = 7;
    private const string PeriodKeyFormat = "yyyy-MM";

    public string Kind => AgentTriggerKinds.AvailabilityGap;

    public string Severity => DaysUntilPeriodStart <= HighSeverityLeadDays
        ? AgentTriggerSeverity.High
        : AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary =>
        ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.AvailabilityGapSummary;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["count"] = AffectedClients.Count.ToString(CultureInfo.InvariantCulture),
        ["from"] = PeriodStart.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["until"] = PeriodEnd.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["names"] = ProactiveNameListRenderer.Render(
            AffectedClients.Select(client => client.ClientName).ToList())
    };

    public string DedupKey => DedupKeyFor(PeriodStart);

    /// <summary>
    /// The DedupKey spelling as a function of its key field, so AvailabilityGapDetector's fingerprint
    /// scan can build the identical key without constructing the event.
    /// </summary>
    public static string DedupKeyFor(DateOnly periodStart) =>
        periodStart.ToString(PeriodKeyFormat, CultureInfo.InvariantCulture);

    public string? ActionRoute => ProactiveActionRoutes.ClientAvailability;

    /// <summary>
    /// The clientId of the per-employee shape is gone: the aggregate names up to ten people, so
    /// preselecting one of them would be an arbitrary choice presented as the finding itself.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Date] = PeriodStart.ToString(
            ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// "names" carries the RENDERED list beside the structured "clients" rows, which is the one piece of
    /// redundancy in this payload and is there on purpose: the ledger refreshes PayloadJson on every
    /// detector tick, and both the reminder sweep and the inbox re-render their sentence from its scalar
    /// entries (ProactiveContentParamMerge). A nested array is not a scalar, so without this the two
    /// would state a current count next to the name list of the day the gap was first detected.
    ///
    /// "clients" is capped at the number of names the sentence can show. Nothing reads it back - the
    /// inbox and the reminder sweep both drop non-scalar payload entries - while the uncapped list was
    /// written into PayloadJson on every detector tick and loaded again on every inbox poll, which for a
    /// workforce-wide gap is the whole workforce carried through both paths for nobody. "count" stays the
    /// full total, so the sentence keeps telling the truth about how many people are affected.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["periodStart"] = PeriodStart,
        ["periodEnd"] = PeriodEnd,
        ["daysUntilPeriodStart"] = DaysUntilPeriodStart,
        ["count"] = AffectedClients.Count,
        ["names"] = ProactiveNameListRenderer.Render(
            AffectedClients.Select(client => client.ClientName).ToList()),
        ["clients"] = AffectedClients.Take(ProactiveNameListRenderer.MaxListedNames).ToList()
    };
}
