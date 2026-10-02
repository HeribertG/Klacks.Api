// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One collective message per root group about the shift days that are still unstaffed in the running pay
/// period (at least the next seven days). Emitted by UnstaffedShiftPeriodDetector; replaces the former
/// one-message-per-shift-day shape, which flooded every planner's inbox with dozens of near-identical rows.
///
/// The dedup key is the group plus the START of the running pay period - state-based, never day-stamped: the
/// ledger row stays open while gaps remain in that period and is resolved by reconcile once they are filled,
/// and a new period opens a new row. The kind stays unstaffed_shift so the rows of the old per-shift shape
/// leave the active fingerprint set on the first tick and resolve automatically.
///
/// The sentence names the first gap day as "from" and the end of the scanned window as "until", so every
/// counted gap lies inside the stated range. The SummaryParams are repeated as scalars in the Payload on
/// purpose: the ledger refreshes PayloadJson on every tick and the inbox and reminder sweep re-render the
/// sentence from those scalars (ProactiveContentParamMerge), so the counts stay current. ActionParams are
/// frozen at the first dispatch (accepted: the link opens the schedule of the group on the first gap day
/// known at that time).
/// </summary>
/// <param name="GroupId">The root group the gaps belong to; narrows the audience to its planners</param>
/// <param name="GroupName">Display name of the root group</param>
/// <param name="PeriodStart">First day of the running pay period, the dedup anchor</param>
/// <param name="PeriodEnd">Last day of the scanned window: max(end of the running period, today + 7)</param>
/// <param name="GapCount">Number of unstaffed shift days (shift x day) in the window</param>
/// <param name="DayCount">Number of distinct calendar days with at least one gap</param>
/// <param name="FirstGapDay">Earliest day with a gap; also the target day of the schedule link</param>
/// <param name="DaysUntilFirstGap">Days from today to FirstGapDay; drives the severity</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record UnstaffedShiftSummaryTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    int GapCount,
    int DayCount,
    DateOnly FirstGapDay,
    int DaysUntilFirstGap) : IAgentTriggerEvent
{
    private const int HighSeverityLeadDays = 3;
    private const int MediumSeverityLeadDays = 7;
    private const string DedupDateFormat = "yyyy-MM-dd";
    private const string GroupParam = "group";
    private const string CountParam = "count";
    private const string DaysParam = "days";
    private const string FromParam = "from";
    private const string UntilParam = "until";

    public string Kind => AgentTriggerKinds.UnstaffedShift;

    public string Severity => DaysUntilFirstGap <= HighSeverityLeadDays ? AgentTriggerSeverity.High
        : DaysUntilFirstGap <= MediumSeverityLeadDays ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;

    public bool PlannersOnly => true;

    public bool RequiresGroupScope => true;

    Guid? IAgentTriggerEvent.GroupId => GroupId;

    public Guid? EntityId => null;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.UnstaffedShiftSummary;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        [GroupParam] = GroupName,
        [CountParam] = GapCount.ToString(CultureInfo.InvariantCulture),
        [DaysParam] = DayCount.ToString(CultureInfo.InvariantCulture),
        [FromParam] = FirstGapDay.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        [UntilParam] = PeriodEnd.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(GroupId, PeriodStart);

    /// <summary>
    /// The DedupKey spelling as a function of its key fields, so the detector's fingerprint scan builds the
    /// identical key without restating the format.
    /// </summary>
    /// <param name="groupId">The root group</param>
    /// <param name="periodStart">First day of the running pay period</param>
    public static string DedupKeyFor(Guid groupId, DateOnly periodStart) =>
        $"{groupId}:{periodStart.ToString(DedupDateFormat, CultureInfo.InvariantCulture)}";

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.GroupId] = GroupId.ToString(),
        [ProactiveActionParamKeys.Date] = FirstGapDay.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload
    {
        get
        {
            var payload = new Dictionary<string, object?>
            {
                ["groupId"] = GroupId,
                ["periodStart"] = PeriodStart,
                ["periodEnd"] = PeriodEnd,
                ["gapCount"] = GapCount,
                ["dayCount"] = DayCount,
                ["firstGapDay"] = FirstGapDay,
                ["daysUntilFirstGap"] = DaysUntilFirstGap
            };
            foreach (var (name, value) in SummaryParams)
            {
                payload[name] = value;
            }

            return payload;
        }
    }
}
