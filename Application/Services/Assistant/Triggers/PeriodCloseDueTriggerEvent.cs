// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fired when a pay-period's close date is within DaysUntilDue days and the period
/// is still open. The close date is the period end plus LagDays; without a stored lag LagDays is 0 and
/// the close date is the period end. With a lag the message switches to PeriodCloseDueWithLag, which names
/// the period end and the close date separately; without one the message and its parameters are exactly the
/// ones from before the lag existed. With AutoCloseDate set (Klacksy will close this period on its own, see
/// PeriodCloseDueDetector) the message switches to PeriodCloseDueAutoClose, which announces the automatic close on
/// that day and how to prevent it; without it nothing changes.
/// </summary>
/// <param name="PeriodEndDate">Last day of the period; identifies the period and is where the action route opens</param>
/// <param name="DaysUntilDue">Days from today to the close date</param>
/// <param name="LagDays">Days after the period end on which the period is closed, 0 when no lag is stored</param>
/// <param name="AutoCloseDate">First day Klacksy's automatic close may run; null when no automatic close is announced</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record PeriodCloseDueTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodEndDate,
    int DaysUntilDue,
    int LagDays = 0,
    DateOnly? AutoCloseDate = null) : IAgentTriggerEvent
{
    public DateOnly CloseDate => PeriodEndDate.AddDays(LagDays);

    public string Kind => AgentTriggerKinds.PeriodCloseDue;
    public string Severity => DaysUntilDue <= 1 ? AgentTriggerSeverity.High
        : DaysUntilDue <= 3 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix
        + (AutoCloseDate != null ? ProactiveMessageI18nKeys.PeriodCloseDueAutoClose
            : LagDays > 0 ? ProactiveMessageI18nKeys.PeriodCloseDueWithLag
            : ProactiveMessageI18nKeys.PeriodCloseDue);

    public IReadOnlyDictionary<string, string> SummaryParams
    {
        get
        {
            if (AutoCloseDate is { } autoCloseDate)
            {
                return new Dictionary<string, string>
                {
                    ["group"] = GroupName,
                    ["periodEnd"] = PeriodEndDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
                    ["date"] = autoCloseDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
                    ["days"] = (DaysUntilDue + autoCloseDate.DayNumber - CloseDate.DayNumber).ToString(CultureInfo.InvariantCulture)
                };
            }

            var summaryParams = new Dictionary<string, string>
            {
                ["group"] = GroupName,
                ["date"] = CloseDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
                ["days"] = DaysUntilDue.ToString(CultureInfo.InvariantCulture)
            };
            if (LagDays > 0)
            {
                summaryParams["periodEnd"] = PeriodEndDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture);
            }

            return summaryParams;
        }
    }

    public static string DedupKeyFor(Guid groupId, DateOnly periodEndDate) =>
        $"{groupId}:{periodEndDate:yyyy-MM-dd}";

    public string DedupKey => DedupKeyFor(GroupId, PeriodEndDate);

    // Bridges the record's non-nullable GroupId to the interface's nullable member: a plain public
    // property of type Guid does not implicitly satisfy a Guid? interface member.
    Guid? IAgentTriggerEvent.GroupId => GroupId;

    public string? ActionRoute => ProactiveActionRoutes.PeriodClosing;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.GroupId] = GroupId.ToString(),
        [ProactiveActionParamKeys.Date] = PeriodEndDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload
    {
        get
        {
            var payload = new Dictionary<string, object?>
            {
                ["groupId"] = GroupId,
                ["groupName"] = GroupName,
                ["periodEndDate"] = PeriodEndDate,
                ["daysUntilDue"] = DaysUntilDue
            };
            if (LagDays > 0)
            {
                payload["lagDays"] = LagDays;
                payload["closeDate"] = CloseDate;
            }

            if (AutoCloseDate is { } autoCloseDate)
            {
                payload["autoCloseDate"] = autoCloseDate;
            }

            return payload;
        }
    }
}
