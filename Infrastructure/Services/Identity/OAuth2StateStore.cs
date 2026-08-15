// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Database-backed, single-use store for OAuth2 state values with a short time-to-live, protecting
/// the authorization code flow against login CSRF (an attacker injecting their own authorization
/// code/state into a victim's browser). The state lives in a table rather than in process memory so
/// the authorization redirect and the callback may be served by different API instances; with an
/// in-process store a callback that lands elsewhere finds nothing and the login fails. Consumption
/// deletes the row and treats a lost delete race as "already consumed", which is what keeps the
/// value single-use across instances -- the same guard PendingConfirmationRepository relies on.
/// Expired rows are collected and removed through the change tracker rather than by a set-based
/// delete: abandoned logins leave at most a handful of rows inside the short time-to-live, and it
/// keeps the store testable against the in-memory provider, matching PruneExpiredAsync next door.
/// </summary>

using System.Security.Cryptography;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Identity;

public class OAuth2StateStore : IOAuth2StateStore
{
    public const int StateTimeToLiveMinutes = 10;
    private const int NonceByteLength = 32;

    private readonly DataBaseContext _context;
    private readonly TimeProvider _timeProvider;

    public OAuth2StateStore(DataBaseContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<string> CreateStateAsync(Guid providerId, CancellationToken cancellationToken = default)
    {
        var nowUtc = _timeProvider.GetUtcNow();
        var expired = await _context.OAuth2States
            .Where(row => row.ExpiresAtUtc < nowUtc.UtcDateTime)
            .ToListAsync(cancellationToken);
        if (expired.Count > 0)
        {
            _context.OAuth2States.RemoveRange(expired);
        }

        var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(NonceByteLength));
        var state = $"{providerId}_{nonce}";

        await _context.OAuth2States.AddAsync(
            new OAuth2StateRow
            {
                Id = Guid.NewGuid(),
                State = state,
                ExpiresAtUtc = nowUtc.AddMinutes(StateTimeToLiveMinutes).UtcDateTime
            },
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return state;
    }

    public async Task<bool> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken = default)
    {
        var row = await _context.OAuth2States.FirstOrDefaultAsync(r => r.State == state, cancellationToken);
        if (row == null)
        {
            return false;
        }

        _context.OAuth2States.Remove(row);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }

        return row.ExpiresAtUtc >= _timeProvider.GetUtcNow().UtcDateTime;
    }
}
