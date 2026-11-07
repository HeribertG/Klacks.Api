// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// In-memory register of the Idempotency-Key requests of this instance. The first request of a key becomes the owner of
/// an entry that its waiters share; the owner either completes it with the answer (kept for ResponseLifetime) or abandons
/// it, which removes the entry and wakes the waiters with null so one of them runs the action itself. The store owns its
/// own size-limited cache, so a flood of keys never touches the shared application cache. It keeps at most
/// MaxEntriesPerUser live entries per user and MaxStoredResponses in total; a request over either limit gets no entry and
/// runs as without the header. A rejected insert makes the cache compact itself in the background, which can evict
/// entries of other users as well (expired ones first); their repeats then run the action again.
/// Singleton without dependencies.
/// </summary>

using Klacks.Api.Application.Constants;
using Microsoft.Extensions.Caching.Memory;

namespace Klacks.Api.Presentation.Filters;

public sealed class IdempotentRequestStore : IDisposable
{
    private const int EntrySize = 1;

    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = IdempotentRequestConstants.MaxStoredResponses,
    });

    private readonly Dictionary<string, List<string>> _keysByUser = new(StringComparer.Ordinal);

    private readonly Lock _gate = new();

    /// <param name="userId">Authenticated user the request belongs to</param>
    /// <param name="key">User, route, Idempotency-Key and body hash of the request</param>
    /// <returns>
    /// Entry null: no entry could be kept, the caller runs the action as without the header. Otherwise IsOwner true when
    /// the caller must run the action and then complete or abandon the entry, false when it waits for the entry.
    /// </returns>
    public (bool IsOwner, TaskCompletionSource<IdempotentResponse?>? Entry) Begin(string userId, string key)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out TaskCompletionSource<IdempotentResponse?>? existing) && existing is not null)
            {
                return (false, existing);
            }

            var userKeys = LiveKeysOf(userId);
            if (userKeys.Count >= IdempotentRequestConstants.MaxEntriesPerUser)
            {
                return (true, null);
            }

            var entry = new TaskCompletionSource<IdempotentResponse?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _cache.Set(key, entry, new MemoryCacheEntryOptions
            {
                Size = EntrySize,
                AbsoluteExpirationRelativeToNow = IdempotentRequestConstants.ResponseLifetime,
            });
            if (!_cache.TryGetValue(key, out _))
            {
                return (true, null);
            }

            userKeys.Add(key);
            _keysByUser[userId] = userKeys;
            return (true, entry);
        }
    }

    public void Complete(TaskCompletionSource<IdempotentResponse?> entry, IdempotentResponse response)
    {
        entry.TrySetResult(response);
    }

    public void Abandon(string key, TaskCompletionSource<IdempotentResponse?> entry)
    {
        lock (_gate)
        {
            if (_cache.TryGetValue(key, out TaskCompletionSource<IdempotentResponse?>? current) && ReferenceEquals(current, entry))
            {
                _cache.Remove(key);
            }
        }

        entry.TrySetResult(null);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private List<string> LiveKeysOf(string userId)
    {
        if (!_keysByUser.TryGetValue(userId, out var keys))
        {
            return [];
        }

        keys.RemoveAll(key => !_cache.TryGetValue(key, out _));
        if (keys.Count == 0)
        {
            _keysByUser.Remove(userId);
        }

        return keys;
    }
}
