// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired once the background bulk-sealing job (SealOpenOrdersJobBackgroundService) has run
/// SealOpenOrdersCommand to completion. Reaches only the user who asked Klacksy to seal the orders —
/// the chat reply returned immediately with a job id when the batch exceeded
/// SealOpenOrdersSkill.SealOpenOrdersSynchronousLimit, so this is how that user learns the outcome.
/// Per-order failures are already isolated and counted by the handler (one transaction per order); this
/// event reports the aggregate plus a short, capped sample so the message can never grow unbounded — see
/// ProactiveTriggerDispatchLimits.ContentParamsJsonMaxLength, which drops the WHOLE params payload
/// (not just the offending field) once the serialized params exceed it.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record BulkSealOrdersCompletedTriggerEvent(
    Guid JobId,
    Guid UserId,
    int TotalOrders,
    int SealedCount,
    int BlockedCount,
    int FailedCount,
    TimeSpan Duration,
    IReadOnlyList<(string OrderName, string Reason)> FailureSample) : IAgentTriggerEvent
{
    private const int MaxFailureSamples = 3;
    private const int MaxReasonLength = 80;
    private const string TruncationSuffix = "…";

    public string Kind => AgentTriggerKinds.BulkSealOrdersCompleted;

    public string Severity => AgentTriggerSeverity.Medium;

    public Guid? TargetUserId => UserId;

    /// <summary>
    /// Has no effect on WHO receives this event — TargetUserId already narrows ResolveRecipientsAsync to
    /// exactly UserId before PlannersOnly is read. Set purely so ProactiveLivePushPolicy.IsCompanionEvent
    /// (!PlannersOnly &amp;&amp; !AdminOnly) does not classify a Medium-severity note as companion chatter,
    /// which IsLoudEvent admits regardless of severity — the same reasoning ScenarioPreparedTriggerEvent
    /// documents for its own TargetUserId event. Without it, a completion note would live-push into an
    /// open chat even though Severity=Medium was chosen specifically to keep it inbox-only there.
    /// </summary>
    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.BulkSealOrdersCompleted;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["sealed"] = SealedCount.ToString(CultureInfo.InvariantCulture),
        ["blocked"] = BlockedCount.ToString(CultureInfo.InvariantCulture),
        ["failed"] = FailedCount.ToString(CultureInfo.InvariantCulture),
        ["total"] = TotalOrders.ToString(CultureInfo.InvariantCulture),
        ["duration"] = Duration.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture),
        ["failureSample"] = BuildFailureSample()
    };

    public string DedupKey => $"{JobId}:completed";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["jobId"] = JobId,
        ["totalOrders"] = TotalOrders,
        ["sealedCount"] = SealedCount,
        ["blockedCount"] = BlockedCount,
        ["failedCount"] = FailedCount,
        ["durationSeconds"] = Duration.TotalSeconds
    };

    private string BuildFailureSample()
    {
        if (FailureSample.Count == 0)
        {
            return string.Empty;
        }

        var entries = FailureSample.Take(MaxFailureSamples).Select(f =>
            $"'{f.OrderName}': {Truncate(f.Reason)}");
        return " " + string.Join("; ", entries) + ".";
    }

    private static string Truncate(string reason) =>
        reason.Length <= MaxReasonLength ? reason : reason[..MaxReasonLength] + TruncationSuffix;
}
