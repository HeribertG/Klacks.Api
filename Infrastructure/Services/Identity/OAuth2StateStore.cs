// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Thread-safe in-process, single-use store for OAuth2 state values with a short time-to-live,
/// protecting the authorization code flow against login CSRF (an attacker injecting their own
/// authorization code/state into a victim's browser). Mirrors the pending-token pattern already
/// used elsewhere in this codebase (e.g. PersistentPendingCompanyRuleDraftStore): a ConcurrentDictionary
/// keyed by the issued value itself, with an explicit expiry checked on consumption instead of relying
/// on IMemoryCache eviction timing.
/// </summary>

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Klacks.Api.Domain.Interfaces.Authentification;

namespace Klacks.Api.Infrastructure.Services.Identity;

public class OAuth2StateStore : IOAuth2StateStore
{
    public const int StateTimeToLiveMinutes = 10;
    private const int NonceByteLength = 32;

    private readonly ConcurrentDictionary<string, DateTimeOffset> _issuedStates = new();
    private readonly TimeProvider _timeProvider;

    public OAuth2StateStore(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public string CreateState(Guid providerId)
    {
        PruneExpired();

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(NonceByteLength));
        var state = $"{providerId}_{nonce}";

        _issuedStates[state] = _timeProvider.GetUtcNow().AddMinutes(StateTimeToLiveMinutes);

        return state;
    }

    public bool ValidateAndConsume(string state)
    {
        if (!_issuedStates.TryRemove(state, out var expiresAtUtc))
        {
            return false;
        }

        return expiresAtUtc >= _timeProvider.GetUtcNow();
    }

    private void PruneExpired()
    {
        var now = _timeProvider.GetUtcNow();
        foreach (var (key, expiresAtUtc) in _issuedStates)
        {
            if (expiresAtUtc < now)
            {
                _issuedStates.TryRemove(key, out _);
            }
        }
    }
}
