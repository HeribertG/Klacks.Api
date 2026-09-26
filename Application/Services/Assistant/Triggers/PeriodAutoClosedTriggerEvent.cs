// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when Klacksy sealed a group's period on its own under full autonomy and the read-back confirmed the
/// seal. Tells the planners that the period is now closed - and that the group-scoped PeriodClosedEvent, which
/// triggers the payroll export, was raised; whether that export succeeded is not known here - and keeps the
/// unattended seal visible next to the PeriodAuditLog entry recorded under the deciding admin.
/// </summary>
/// <param name="PeriodStartDate">First day of the sealed period</param>
/// <param name="PeriodEndDate">Last day of the sealed period; identifies it</param>
/// <param name="LagDays">The stored close lag the close date was computed from</param>
/// <param name="DecidingAdminUserId">The admin whose standing consent released the seal</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PeriodAutoClosedTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    int LagDays,
    Guid DecidingAdminUserId) : IAgentTriggerEvent
{
    private const string ClosedDedupSuffix = ":auto-closed";
    private const string PeriodDedupFormat = "yyyy-MM-dd";

    public string Kind => AgentTriggerKinds.PeriodAutoClose;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.PeriodAutoClosed;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["from"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["until"] = PeriodEndDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey =>
        $"{GroupId}:{PeriodEndDate.ToString(PeriodDedupFormat, CultureInfo.InvariantCulture)}{ClosedDedupSuffix}";

    // Bridges the record's non-nullable GroupId to the interface's nullable member: a plain public
    // property of type Guid does not implicitly satisfy a Guid? interface member.
    Guid? IAgentTriggerEvent.GroupId => GroupId;

    public string? ActionRoute => ProactiveActionRoutes.PeriodClosing;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.GroupId] = GroupId.ToString(),
        [ProactiveActionParamKeys.Date] = PeriodEndDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["groupId"] = GroupId,
        ["groupName"] = GroupName,
        ["periodStartDate"] = PeriodStartDate,
        ["periodEndDate"] = PeriodEndDate,
        ["lagDays"] = LagDays,
        ["decidingAdminUserId"] = DecidingAdminUserId,
        ["autoClosed"] = true
    };
}
