// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One planner's daily condition-ledger digest, dispatched through the ordinary proactive trigger
/// pipeline (IAgentTriggerService.OnEventAsync) exactly like any other kind - persisted as an inbox row,
/// live-pushed when the planner is connected and severity-eligible, and offered to the messenger channel
/// under MessengerWakeUpPolicy. Targeted at a single user (TargetUserId) so AgentConditionDigestService
/// can build one event per planner with that planner's own scoped counts, and PlannersOnly is set
/// alongside it purely to classify the event correctly for IsLoudEvent - the audience is always the one
/// TargetUserId, TargetUserId already short-circuits ResolveRecipientsAsync before PlannersOnly is
/// consulted. Not condition-ledger-tracked (AgentConditionLedgerPolicy.IsLedgerTracked requires
/// TargetUserId to be null): this is an aggregating event with no single fingerprint of its own.
/// TotalCount is caller-supplied, deliberately NOT HighCount+MediumCount+LowCount: the three buckets
/// are counted from AgentConditionRepository.GetOpenForScopeAsync's capped result, so on a scope with
/// more open findings than the cap the bucket sum would silently undercount. AgentConditionDigestService
/// falls back to the repository's uncapped CountOpenForScopeAsync for TotalCount whenever the cap was
/// hit, so the rendered "N open findings" sentence can never claim fewer findings than actually exist,
/// even while the severity breakdown remains a best-effort read of the capped sample.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record AgentConditionDigestTriggerEvent(
    Guid PlannerUserId,
    DateOnly LocalDigestDate,
    int TotalCount,
    int HighCount,
    int MediumCount,
    int LowCount,
    int NewCount,
    IReadOnlyList<AgentConditionDigestFinding> TopFindings) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.DailyDigest;

    public Guid? TargetUserId => PlannerUserId;

    public bool PlannersOnly => true;

    /// <summary>
    /// High whenever the planner's scope contains at least one High-severity finding - this is what lets
    /// MessengerWakeUpPolicy offer the digest over the messenger channel, and what the frontend needs to
    /// render the [HIGH] emphasis AgentTriggerService.FormatMessage adds for non-i18n callers.
    /// </summary>
    public string Severity =>
        HighCount > 0 ? AgentTriggerSeverity.High
        : MediumCount > 0 ? AgentTriggerSeverity.Medium
        : AgentTriggerSeverity.Low;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.DailyDigest;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["totalCount"] = TotalCount.ToString(CultureInfo.InvariantCulture),
        ["highCount"] = HighCount.ToString(CultureInfo.InvariantCulture),
        ["mediumCount"] = MediumCount.ToString(CultureInfo.InvariantCulture),
        ["lowCount"] = LowCount.ToString(CultureInfo.InvariantCulture),
        ["newCount"] = NewCount.ToString(CultureInfo.InvariantCulture)
    };

    /// <summary>Per calendar day, so tomorrow's digest is never suppressed by today's already-persisted dispatch row for the same user+kind.</summary>
    public string DedupKey => DedupKeyFor(LocalDigestDate);

    public static string DedupKeyFor(DateOnly localDigestDate) => localDigestDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["plannerUserId"] = PlannerUserId,
        ["digestDate"] = LocalDigestDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        ["highCount"] = HighCount,
        ["mediumCount"] = MediumCount,
        ["lowCount"] = LowCount,
        ["newCount"] = NewCount,
        ["topFindings"] = TopFindings
            .Select(finding => new Dictionary<string, object?>
            {
                ["triggerKind"] = finding.TriggerKind,
                ["entityId"] = finding.EntityId,
                ["groupId"] = finding.GroupId,
                ["severity"] = finding.Severity,
                ["detectedAtUtc"] = finding.DetectedAtUtc
            })
            .ToList()
    };
}
