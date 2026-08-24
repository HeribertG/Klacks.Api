// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when a plannable employee has not entered a single availability for the upcoming
/// calendar month. Severity rises to high once the month start is at most
/// HighSeverityLeadDays away.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record AvailabilityGapTriggerEvent(
    Guid ClientId,
    string ClientName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int DaysUntilPeriodStart) : IAgentTriggerEvent
{
    private const int HighSeverityLeadDays = 7;

    public string Kind => AgentTriggerKinds.AvailabilityGap;

    public string Severity => DaysUntilPeriodStart <= HighSeverityLeadDays
        ? AgentTriggerSeverity.High
        : AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.AvailabilityGap;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["name"] = ClientName,
        ["from"] = PeriodStart.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["until"] = PeriodEnd.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(ClientId, PeriodStart);

    public Guid? EntityId => ClientId;

    /// <summary>
    /// The DedupKey spelling as a function of its key fields, so AvailabilityGapDetector's uncapped
    /// fingerprint scan can build the identical key without restating the format.
    /// </summary>
    public static string DedupKeyFor(Guid clientId, DateOnly periodStart) => $"{clientId}:{periodStart:yyyy-MM}";

    public string? ActionRoute => ProactiveActionRoutes.ClientAvailability;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.ClientId] = ClientId.ToString(),
        [ProactiveActionParamKeys.Date] = PeriodStart.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["clientId"] = ClientId,
        ["clientName"] = ClientName,
        ["periodStart"] = PeriodStart,
        ["periodEnd"] = PeriodEnd,
        ["daysUntilPeriodStart"] = DaysUntilPeriodStart
    };
}
