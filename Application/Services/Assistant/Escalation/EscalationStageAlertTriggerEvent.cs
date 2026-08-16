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

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public sealed record EscalationStageAlertTriggerEvent(
    Guid StageId,
    string UserId,
    string EmployeeName,
    DateTime ShiftStartUtc,
    DateTime DueAtUtc) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.EscalationStageAlert;
    public string Severity => AgentTriggerSeverity.High;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.EscalationStageAlert;

    public Guid? TargetUserId => Guid.TryParse(UserId, out var id) ? id : null;

    /// <summary>
    /// dueTime is rendered in UTC, not the installation's configured timezone: no reusable UTC-to-local
    /// conversion exists outside ICompanyClock, which only resolves a date, not a clock time. Same
    /// simplification as E64 (docs/PLAN-messenger-ausfallmeldung-eskalation-2026-08-15.md) - the
    /// assumption is marked here rather than silently guessed at.
    /// </summary>
    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["employee"] = EmployeeName,
        ["date"] = ShiftStartUtc.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["dueTime"] = DueAtUtc.ToString("HH:mm", CultureInfo.InvariantCulture) + " UTC"
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
