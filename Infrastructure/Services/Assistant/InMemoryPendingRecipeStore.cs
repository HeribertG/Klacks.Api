// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe in-memory store for recipes paused on an ask step. Entries are keyed by user and
/// conversation, expire after RecipeEngineDefaults.PendingRecipeTtlMinutes and are pruned
/// opportunistically on each Save. A new Save for the same key replaces the previous pause.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class InMemoryPendingRecipeStore : IPendingRecipeStore
{
    private readonly ConcurrentDictionary<string, PendingRecipe> _pending = new();

    public void Save(PendingRecipe pending)
    {
        PruneExpired();
        pending.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(RecipeEngineDefaults.PendingRecipeTtlMinutes);
        _pending[BuildKey(pending.UserId, pending.ConversationId)] = pending;
    }

    public PendingRecipe? Peek(Guid userId, string conversationId)
    {
        if (!_pending.TryGetValue(BuildKey(userId, conversationId), out var entry))
        {
            return null;
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
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

    private void PruneExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var (key, entry) in _pending)
        {
            if (entry.ExpiresAtUtc < now)
            {
                _pending.TryRemove(key, out _);
            }
        }
    }
}
