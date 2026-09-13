// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired once per scan when at least one employee's accumulated TargetHours deviation exceeds the
/// threshold (default ±12h). Deliberately ONE event for the whole workforce rather than one per employee:
/// the per-employee shape produced one dispatch row per employee per recipient (145 rows for one admin on
/// 2026-09-03), which is not a notification but a mailing. The dedup key is the period alone; the
/// per-user half of "once per user per period" comes from the dispatch row's own user_id column.
/// The sentence is its own i18n key (TargetHoursDriftSummary), never the per-employee one: that sentence
/// names a single person and a single deviation, so filling it with a name list and the worst drift would
/// state something untrue about everybody else it names.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record TargetHoursDriftTriggerEvent(
    IReadOnlyList<TargetHoursDriftAffectedClient> AffectedClients,
    string PeriodLabel) : IAgentTriggerEvent
{
    private const decimal HighSeverityDriftHours = 24m;
    private const decimal MediumSeverityDriftHours = 12m;
    private const int MaxListedNames = 10;
    private const string NameSeparator = ", ";
    private const string OverflowFormat = "{0} +{1}";
    private const string DriftFormat = "+0.0;-0.0;0";

    public string Kind => AgentTriggerKinds.TargetHoursDrift;

    public string Severity => LargestAbsoluteDrift() >= HighSeverityDriftHours ? AgentTriggerSeverity.High
        : LargestAbsoluteDrift() >= MediumSeverityDriftHours ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;

    public bool PlannersOnly => true;

    public string Summary =>
        ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.TargetHoursDriftSummary;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["count"] = AffectedClients.Count.ToString(CultureInfo.InvariantCulture),
        ["period"] = PeriodLabel,
        ["hours"] = LargestSignedDrift().ToString(DriftFormat, CultureInfo.InvariantCulture),
        ["names"] = RenderNames()
    };

    public string DedupKey => DedupKeyFor(PeriodLabel);

    /// <summary>
    /// The DedupKey spelling as a function of its key field, so TargetHoursDriftDetector's fingerprint
    /// scan can build the identical key without constructing the event.
    /// </summary>
    public static string DedupKeyFor(string periodLabel) => periodLabel;

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Period] = PeriodLabel
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["periodLabel"] = PeriodLabel,
        ["count"] = AffectedClients.Count,
        ["clients"] = AffectedClients
    };

    private decimal LargestAbsoluteDrift() =>
        AffectedClients.Count == 0 ? 0m : AffectedClients.Max(client => Math.Abs(client.DriftHours));

    private decimal LargestSignedDrift()
    {
        if (AffectedClients.Count == 0)
        {
            return 0m;
        }

        var worst = AffectedClients[0];
        foreach (var client in AffectedClients)
        {
            if (Math.Abs(client.DriftHours) > Math.Abs(worst.DriftHours))
            {
                worst = client;
            }
        }

        return worst.DriftHours;
    }

    private string RenderNames()
    {
        var listed = string.Join(
            NameSeparator, AffectedClients.Take(MaxListedNames).Select(client => client.ClientName));

        return AffectedClients.Count <= MaxListedNames
            ? listed
            : string.Format(
                CultureInfo.InvariantCulture, OverflowFormat, listed, AffectedClients.Count - MaxListedNames);
    }
}
