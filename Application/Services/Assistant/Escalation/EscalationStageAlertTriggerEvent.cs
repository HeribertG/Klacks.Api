// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The event EscalationNotifier hands to IProactiveMessengerTextComposer so it can render this
/// stage's wake-up sentence. Never dispatched through AgentTriggerService.OnEventAsync - the
/// escalation chain has its own narrow delivery path (docs/ENTWURF-eskalationskette-2026-08-16.md
/// §2) - so TargetUserId/PlannersOnly/AdminOnly are set for documentation only and are never read by
/// a recipient resolver.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public sealed record EscalationStageAlertTriggerEvent(
    Guid StageId,
    string UserId,
    string EmployeeName,
    DateTime ShiftStartUtc,
    DateTime DueAtUtc,
    TimeZoneInfo CompanyTimeZone) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.EscalationStageAlert;
    public string Severity => AgentTriggerSeverity.High;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.EscalationStageAlert;

    public Guid? TargetUserId => Guid.TryParse(UserId, out var id) ? id : null;

    /// <summary>
    /// date and dueTime are rendered in the company's configured time zone (via ICompanyClock, resolved
    /// once by EscalationNotifier.NotifyStageAsync) instead of raw UTC, so a shift starting near midnight
    /// in a positive-offset zone reports the correct local calendar day. The zone id is appended to
    /// dueTime so the recipient is never left guessing which zone the clock time is in.
    /// </summary>
    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["employee"] = EmployeeName,
        ["date"] = TimeZoneInfo.ConvertTimeFromUtc(ShiftStartUtc, CompanyTimeZone)
            .ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["dueTime"] = TimeZoneInfo.ConvertTimeFromUtc(DueAtUtc, CompanyTimeZone)
            .ToString("HH:mm", CultureInfo.InvariantCulture) + " " + IanaTimeZoneId.From(CompanyTimeZone)
    };

    public string DedupKey => $"{StageId}:escalation-stage-alert";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["stageId"] = StageId,
        ["employee"] = EmployeeName,
        ["shiftStartUtc"] = ShiftStartUtc,
        ["dueAtUtc"] = DueAtUtc
    };
}
