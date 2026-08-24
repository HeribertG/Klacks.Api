// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when an employee's accumulated TargetHours deviation exceeds the configured threshold
/// (default ±12h). Disponent should rebalance the schedule for that employee.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record TargetHoursDriftTriggerEvent(
    Guid ClientId,
    string ClientName,
    decimal DriftHours,
    string PeriodLabel) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.TargetHoursDrift;
    public string Severity => Math.Abs(DriftHours) >= 24 ? AgentTriggerSeverity.High
        : Math.Abs(DriftHours) >= 12 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.TargetHoursDrift;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["name"] = ClientName,
        ["hours"] = DriftHours.ToString("+0.0;-0.0;0", CultureInfo.InvariantCulture),
        ["period"] = PeriodLabel
    };

    // Dedup once per employee + period (magnitude-independent): the same drift is alerted at most once.
    public string DedupKey => DedupKeyFor(ClientId, PeriodLabel);

    /// <summary>
    /// The DedupKey spelling as a function of its key fields, so TargetHoursDriftDetector's
    /// fingerprint scan can build the identical key from a key-only projection instead of restating
    /// the format.
    /// </summary>
    public static string DedupKeyFor(Guid clientId, string periodLabel) => $"{clientId}:{periodLabel}";

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.ClientId] = ClientId.ToString(),
        [ProactiveActionParamKeys.Period] = PeriodLabel
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["clientId"] = ClientId,
        ["clientName"] = ClientName,
        ["driftHours"] = DriftHours,
        ["periodLabel"] = PeriodLabel
    };
}
