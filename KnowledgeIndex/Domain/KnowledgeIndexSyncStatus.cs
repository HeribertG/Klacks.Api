// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Domain;

/// <summary>
/// Point-in-time view of the background knowledge index synchronization. LastCompletedUtc and
/// LastFailedUtc are kept side by side rather than cleared by the next run, so the newer of the two
/// tells whether the most recent run succeeded.
/// </summary>
/// <param name="IsRunning">True while a synchronization run executes.</param>
/// <param name="IsPending">True when at least one request arrived that no started run has picked up yet.</param>
/// <param name="LastCompletedUtc">End of the last successful run; null if none succeeded in this process.</param>
/// <param name="LastFailedUtc">End of the last failed run; null if none failed in this process.</param>
/// <param name="LastReason">Reason of the most recently started run, as passed by the requester.</param>
/// <param name="LastError">Exception message of the last failed run; null if none failed.</param>
public sealed record KnowledgeIndexSyncStatus(
    bool IsRunning,
    bool IsPending,
    DateTimeOffset? LastCompletedUtc,
    DateTimeOffset? LastFailedUtc,
    string? LastReason,
    string? LastError);
