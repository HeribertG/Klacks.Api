// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Database-backed single-use store for OAuth authorization codes. Codes are bound to the issuing
/// client, redirect URI and PKCE challenge, expire after a short TTL and are consumed atomically so
/// that a code can never be redeemed twice. The codes live in a table rather than in process memory
/// so the authorize call and the token exchange may be served by different API instances; with an
/// in-process store an exchange that lands elsewhere is rejected as invalid_grant even though the
/// client behaved correctly, and a restart voids every outstanding code. Redemption deletes the row
/// and treats a lost delete race as "already redeemed".
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Authentication;

public class OAuthAuthorizationCodeStore : IOAuthAuthorizationCodeStore
{
    private readonly DataBaseContext _context;
    private readonly TimeProvider _timeProvider;

    public OAuthAuthorizationCodeStore(DataBaseContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task StoreAsync(string code, OAuthAuthorizationCodeData data, CancellationToken cancellationToken = default)
    {
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var expired = await _context.OAuthAuthorizationCodes
            .Where(row => row.ExpiresAtUtc <= utcNow)
            .ToListAsync(cancellationToken);
        if (expired.Count > 0)
        {
            _context.OAuthAuthorizationCodes.RemoveRange(expired);
        }

        await _context.OAuthAuthorizationCodes.AddAsync(
            new OAuthAuthorizationCodeRow
            {
                Id = Guid.NewGuid(),
                Code = code,
                UserId = data.UserId,
                ClientId = data.ClientId,
                ClientName = data.ClientName,
                RedirectUri = data.RedirectUri,
                CodeChallenge = data.CodeChallenge,
                Scope = data.Scope,
                ExpiresAtUtc = utcNow.Add(OAuthConstants.AuthorizationCodeLifetime)
            },
            cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OAuthAuthorizationCodeData?> ConsumeAsync(string code, CancellationToken cancellationToken = default)
    {
        var row = await _context.OAuthAuthorizationCodes.FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
        if (row == null)
        {
            return null;
        }

        _context.OAuthAuthorizationCodes.Remove(row);
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;
        }

        if (row.ExpiresAtUtc <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            return null;
        }

        return new OAuthAuthorizationCodeData(
            row.UserId,
            row.ClientId,
            row.ClientName,
            row.RedirectUri,
            row.CodeChallenge,
            row.Scope);
    }
}
