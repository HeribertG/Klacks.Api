// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when Klacksy was allowed to close a group's period on its own but did NOT, with the cause. The
/// period stays open and a person has to look at it; a silent non-close would leave the planners believing
/// the period (and its payroll export) was done. Reason is part of the DedupKey, so the tick that re-offers
/// this event every scan reaches each recipient once per group, period and cause.
/// </summary>
/// <param name="PeriodStartDate">First day of the period that stays open</param>
/// <param name="PeriodEndDate">Last day of the period that stays open; identifies it</param>
/// <param name="ErrorCount">Open errors of the period; only meaningful for OpenErrors, zero otherwise</param>
/// <param name="Reason">Why the period was not closed</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PeriodAutoCloseBlockedTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    int ErrorCount,
    PeriodAutoCloseBlockReason Reason) : IAgentTriggerEvent
{
    private const string BlockedDedupSegment = ":auto-close-blocked:";
    private const string PeriodDedupFormat = "yyyy-MM-dd";

    public string Kind => AgentTriggerKinds.PeriodAutoClose;

    public string Severity => Reason is PeriodAutoCloseBlockReason.Failed or PeriodAutoCloseBlockReason.NotVerified
        ? AgentTriggerSeverity.High
        : AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + I18nKeyFor(Reason);

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["from"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["until"] = PeriodEndDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["errors"] = ErrorCount.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey =>
        $"{GroupId}:{PeriodEndDate.ToString(PeriodDedupFormat, CultureInfo.InvariantCulture)}{BlockedDedupSegment}{Reason}";

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
        ["errorCount"] = ErrorCount,
        ["blockReason"] = Reason.ToString(),
        ["autoClosed"] = false
    };

    public static string I18nKeyFor(PeriodAutoCloseBlockReason reason) => reason switch
    {
        PeriodAutoCloseBlockReason.NoLagStored => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedNoLag,
        PeriodAutoCloseBlockReason.CloseWindowMissed => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedWindowMissed,
        PeriodAutoCloseBlockReason.PartiallySealed => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedPartiallySealed,
        PeriodAutoCloseBlockReason.OpenErrors => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedErrors,
        PeriodAutoCloseBlockReason.AutonomyLowered => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedAutonomyLowered,
        PeriodAutoCloseBlockReason.Refused => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedFailed,
        PeriodAutoCloseBlockReason.Failed => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedFailed,
        PeriodAutoCloseBlockReason.NotVerified => ProactiveMessageI18nKeys.PeriodAutoCloseBlockedFailed,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown period auto-close block reason.")
    };
}
