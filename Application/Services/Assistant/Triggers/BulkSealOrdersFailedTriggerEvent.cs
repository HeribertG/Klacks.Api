// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired when the background bulk-sealing job (SealOpenOrdersJobBackgroundService) aborts with an
/// unhandled exception instead of returning a SealOpenOrdersResult — e.g. the initial order query or the
/// auto-assign step throws, neither of which is isolated per order the way the sealing loop itself is.
/// Distinct from BulkSealOrdersCompletedTriggerEvent (which always fires, even with per-order failures
/// inside FailedCount) because this means the run never produced a result at all: whatever was already
/// sealed before the abort stays sealed (one transaction per order), everything after it is untouched.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record BulkSealOrdersFailedTriggerEvent(
    Guid JobId,
    Guid UserId,
    string ErrorMessage) : IAgentTriggerEvent
{
    private const int MaxErrorLength = 200;
    private const string TruncationSuffix = "…";

    public string Kind => AgentTriggerKinds.BulkSealOrdersFailed;

    public string Severity => AgentTriggerSeverity.High;

    public Guid? TargetUserId => UserId;

    /// <summary>
    /// seal_open_orders is restricted to users with unrestricted group scope, so its acting user is
    /// always admin-tier; this only affects ProactiveLivePushPolicy's companion classification (already
    /// loud here regardless, since Severity is High), kept for consistency with the completed event.
    /// </summary>
    public bool AdminOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.BulkSealOrdersFailed;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["error"] = ErrorMessage.Length <= MaxErrorLength
            ? ErrorMessage
            : ErrorMessage[..MaxErrorLength] + TruncationSuffix
    };

    public string DedupKey => $"{JobId}:failed";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["jobId"] = JobId,
        ["error"] = ErrorMessage
    };
}
