// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe in-memory store for company-rule drafts paused between dialog turns. Entries are keyed by
/// user and conversation and expire after CompanyRuleDraftDefaults.PendingDraftTtlMinutes; every Set
/// (starting a draft or storing accepted parameters) slides the expiration forward by the full TTL, and
/// entries are pruned opportunistically on each Set. A new Set for the same key replaces the previous
/// draft.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class InMemoryPendingCompanyRuleDraftStore : IPendingCompanyRuleDraftStore
{
    private readonly ConcurrentDictionary<string, CompanyRuleDraft> _pending = new();

    public void Set(Guid userId, string conversationId, CompanyRuleDraft draft)
    {
        PruneExpired();
        draft.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(CompanyRuleDraftDefaults.PendingDraftTtlMinutes);
        _pending[BuildKey(userId, conversationId)] = draft;
    }

    public CompanyRuleDraft? Get(Guid userId, string conversationId)
    {
        if (!_pending.TryGetValue(BuildKey(userId, conversationId), out var entry))
        {
            return null;
        }

        if (IsExpired(entry))
        {
            _pending.TryRemove(BuildKey(userId, conversationId), out _);
            return null;
        }

        return entry;
    }

    public void Clear(Guid userId, string conversationId)
    {
        _pending.TryRemove(BuildKey(userId, conversationId), out _);
    }

    private static string BuildKey(Guid userId, string conversationId)
    {
        return $"{userId:N}|{conversationId}";
    }

    private static bool IsExpired(CompanyRuleDraft draft)
    {
        return draft.ExpiresAtUtc < DateTime.UtcNow;
    }

    private void PruneExpired()
    {
        foreach (var (key, entry) in _pending)
        {
            if (IsExpired(entry))
            {
                _pending.TryRemove(key, out _);
            }
        }
    }
}
