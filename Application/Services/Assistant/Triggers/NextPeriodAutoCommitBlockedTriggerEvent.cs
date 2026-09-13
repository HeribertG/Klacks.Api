// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired whenever the FullyAutonomous branch produced (or intended to produce) an autofill scenario for
/// a group's next pay-period but did NOT accept it into the real schedule. The scenario is left as a
/// draft, exactly like the Autonomous branch, and this event asks a human to review it. Every failure
/// path of the watcher raises this event - a silent one would leave the planners believing the plan was
/// committed, which is the one wrong belief this whole branch must never create.
/// <see cref="Reason"/> selects the wording and is part of the DedupKey, so a timeout does not swallow a
/// later compliance block for the same group and period.
/// </summary>
/// <param name="ScenarioId">
/// The draft that stays unaccepted. Null for the reasons that occur BEFORE a final scenario is known -
/// Timeout and NotCommittable - where there is nothing to point a reviewer at yet.
/// </param>
/// <param name="NewIssueCount">Only meaningful for NewViolations; zero for every other reason.</param>
/// <param name="Reason">Why the acceptance was withheld.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record NextPeriodAutoCommitBlockedTriggerEvent(
    Guid GroupId,
    string GroupName,
    DateOnly PeriodStartDate,
    DateOnly PeriodEndDate,
    Guid? ScenarioId,
    int NewIssueCount,
    NextPeriodAutoCommitBlockReason Reason) : IAgentTriggerEvent
{
    private const string CommitBlockedDedupSuffix = ":commit-blocked";
    private const string DedupSeparator = ":";
    private const string PeriodDedupFormat = "yyyy-MM-dd";

    /// <summary>
    /// The prefix every commit outcome of this group and period shares. The detector matches ledger
    /// fingerprints against it to tell "no watcher ever reported an outcome" (an interrupted run) from
    /// "an outcome was already reported" (a run that finished and blocked) - the two are otherwise
    /// indistinguishable, because both leave the scenario a draft and no job in the registry.
    /// </summary>
    public static string CommitOutcomeDedupPrefix(Guid groupId, DateOnly periodStartDate) =>
        $"{groupId}{DedupSeparator}{periodStartDate.ToString(PeriodDedupFormat, CultureInfo.InvariantCulture)}{CommitBlockedDedupSuffix}";

    public string Kind => AgentTriggerKinds.NextPeriodSchedulingDue;

    public string Severity => AgentTriggerSeverity.Medium;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + I18nKeyFor(Reason);

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["group"] = GroupName,
        ["date"] = PeriodStartDate.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
        ["issues"] = NewIssueCount.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>
    /// Group, period and reason, so a timeout does not swallow a later compliance block for the same
    /// period. NewViolations is the one exception and carries NO reason suffix: it is the only reason
    /// that ever fired before the suffix existed, and this keeps its fingerprint backward-compatible for
    /// rows opened before it - a changed spelling would strand every one of those rows open for ever,
    /// because this kind is no IAgentConditionFingerprintSource and its rows are never auto-resolved.
    /// Interrupted additionally carries the scenario id: it is not raised by a watcher for one run but by
    /// the detector for whatever draft it finds, and a second draft for the same period after a restart
    /// is a second finding, not a repeat of the first.
    /// </summary>
    public string DedupKey
    {
        get
        {
            var prefix = CommitOutcomeDedupPrefix(GroupId, PeriodStartDate);
            if (Reason == NextPeriodAutoCommitBlockReason.NewViolations)
            {
                return prefix;
            }

            var key = prefix + DedupSeparator + Reason;

            return Reason == NextPeriodAutoCommitBlockReason.Interrupted
                ? key + DedupSeparator + ScenarioId
                : key;
        }
    }

    // Bridges the record's non-nullable GroupId to the interface's nullable member: a plain public
    // property of type Guid does not implicitly satisfy a Guid? interface member.
    Guid? IAgentTriggerEvent.GroupId => GroupId;

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.GroupId] = GroupId.ToString(),
        [ProactiveActionParamKeys.Date] = PeriodStartDate.ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["groupId"] = GroupId,
        ["groupName"] = GroupName,
        ["periodStartDate"] = PeriodStartDate,
        ["periodEndDate"] = PeriodEndDate,
        ["scenarioId"] = ScenarioId,
        ["newIssueCount"] = NewIssueCount,
        ["blockReason"] = Reason.ToString(),
        ["autoCommitted"] = false
    };

    private static string I18nKeyFor(NextPeriodAutoCommitBlockReason reason) => reason switch
    {
        NextPeriodAutoCommitBlockReason.NewViolations => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlocked,
        NextPeriodAutoCommitBlockReason.Refused => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedRefused,
        NextPeriodAutoCommitBlockReason.Conflict => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedConflict,
        NextPeriodAutoCommitBlockReason.Timeout => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedTimeout,
        NextPeriodAutoCommitBlockReason.NotCommittable => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedNotCommittable,
        NextPeriodAutoCommitBlockReason.KillSwitch => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedKillSwitch,
        NextPeriodAutoCommitBlockReason.AutonomyLowered => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedAutonomyLowered,
        NextPeriodAutoCommitBlockReason.Interrupted => ProactiveMessageI18nKeys.NextPeriodAutoCommitBlockedInterrupted,
        _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, "Unknown auto-commit block reason.")
    };
}
