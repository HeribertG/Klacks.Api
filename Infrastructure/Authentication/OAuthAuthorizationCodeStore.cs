// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory single-use store for OAuth authorization codes. Codes are bound to the issuing
/// client, redirect URI and PKCE challenge, expire after a short TTL and are consumed
/// atomically so that a code can never be redeemed twice.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Infrastructure.Authentication;

public class OAuthAuthorizationCodeStore : IOAuthAuthorizationCodeStore
{
    private readonly ConcurrentDictionary<string, (OAuthAuthorizationCodeData Data, DateTime ExpiresAt)> _codes = new();

    public void Store(string code, OAuthAuthorizationCodeData data)
    {
        PruneExpired();
        _codes[code] = (data, DateTime.UtcNow.Add(OAuthConstants.AuthorizationCodeLifetime));
    }

    public OAuthAuthorizationCodeData? Consume(string code)
    {
        if (!_codes.TryRemove(code, out var entry))
        {
            return null;
        }

        return entry.ExpiresAt <= DateTime.UtcNow ? null : entry.Data;
    }

    private void PruneExpired()
    {
        var utcNow = DateTime.UtcNow;
        foreach (var pair in _codes)
        {
            if (pair.Value.ExpiresAt <= utcNow)
            {
                _codes.TryRemove(pair.Key, out _);
            }
        }
    }
}
