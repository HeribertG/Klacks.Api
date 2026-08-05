// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Short-lived cache of terminal job outcomes (completed/cancelled/failed) so clients that
/// missed the SignalR event (e.g. during a reconnect window) can recover the outcome via
/// a status poll. Entries expire after <see cref="TtlMinutes"/> minutes to bound memory.
/// </summary>
/// <typeparam name="TResult">Result DTO stored for completed jobs.</typeparam>
public sealed class JobTerminalStateCache<TResult>
    where TResult : class
{
    private readonly ConcurrentDictionary<Guid, CacheEntry> _entries = new();

    public int TtlMinutes { get; init; } = 15;

    public void StoreCompleted(Guid jobId, TResult result) =>
        Store(jobId, WizardJobStatusValues.Completed, result, null);

    public void StoreCancelled(Guid jobId) =>
        Store(jobId, WizardJobStatusValues.Cancelled, null, null);

    public void StoreFailed(Guid jobId, string reason) =>
        Store(jobId, WizardJobStatusValues.Failed, null, reason);

    public bool TryGet(Guid jobId, out string status, out TResult? result, out string? reason)
    {
        EvictExpired();
        if (_entries.TryGetValue(jobId, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
        {
            status = entry.Status;
            result = entry.Result;
            reason = entry.Reason;
            return true;
        }

        status = WizardJobStatusValues.Unknown;
        result = null;
        reason = null;
        return false;
    }

    private void Store(Guid jobId, string status, TResult? result, string? reason)
    {
        EvictExpired();

        // First terminal state wins. Job ids are unique per run, so a second store can only come from a
        // late failure path - a completed run must never end up reported as failed.
        _entries.TryAdd(jobId, new CacheEntry(status, result, reason, DateTime.UtcNow.AddMinutes(TtlMinutes)));
    }

    private void EvictExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var kv in _entries)
        {
            if (kv.Value.ExpiresAt <= now)
            {
                _entries.TryRemove(kv.Key, out _);
            }
        }
    }

    private sealed record CacheEntry(
        string Status,
        TResult? Result,
        string? Reason,
        DateTime ExpiresAt);
}
