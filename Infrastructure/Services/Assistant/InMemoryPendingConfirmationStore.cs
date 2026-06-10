// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe in-memory store for one-time skill confirmation tokens. Entries carry the full
/// pending invocation (user, skill, parameters), expire after AutonomyDefaults.ConfirmationTtlMinutes
/// and are removed on consumption; expired entries are pruned opportunistically on each Create.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class InMemoryPendingConfirmationStore : IPendingConfirmationStore
{
    private readonly ConcurrentDictionary<string, PendingConfirmation> _pending = new();

    public string Create(Guid userId, string skillName, IReadOnlyDictionary<string, object> parameters)
    {
        PruneExpired();

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(AutonomyDefaults.ConfirmationTtlMinutes);
        _pending[token] = new PendingConfirmation(
            userId,
            skillName,
            new Dictionary<string, object>(parameters),
            expiresAt);
        return token;
    }

    public PendingConfirmation? Consume(string token, Guid userId, string? expectedSkillName = null)
    {
        if (!_pending.TryRemove(token, out var entry))
        {
            return null;
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            return null;
        }

        if (entry.UserId != userId)
        {
            return null;
        }

        if (expectedSkillName != null
            && !string.Equals(entry.SkillName, expectedSkillName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return entry;
    }

    private void PruneExpired()
    {
        var now = DateTime.UtcNow;
        foreach (var (token, entry) in _pending)
        {
            if (entry.ExpiresAtUtc < now)
            {
                _pending.TryRemove(token, out _);
            }
        }
    }
}
