// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Authentication;

/// <summary>
/// Stores the session refresh tokens (hashed). A refresh does not rotate: the token keeps its value and its expiry slides
/// (ExtendRefreshTokenAsync). Only the migration of a token from local storage issues a new token and leaves the old one
/// valid for a short grace (ReplaceRefreshTokenAsync).
/// </summary>
/// <param name="context">Database context holding the refresh_token table</param>
/// <param name="timeProvider">Clock for expiry stamps and checks</param>
/// <param name="logger">Records lookup failures</param>
public class RefreshTokenService : IRefreshTokenService
{
    private const int MaxRefreshTokensPerUser = 20;

    private readonly DataBaseContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(DataBaseContext context, TimeProvider timeProvider, ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var tokens = await LoadUserTokensAsync(userId);
        _context.RefreshToken.RemoveRange(ExpiredAndOverflow(tokens));

        var (entity, rawToken) = BuildRefreshToken(userId);
        _context.RefreshToken.Add(entity);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<bool> ExtendRefreshTokenAsync(string userId, string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return false;
        }

        var hashedToken = RefreshTokenHasher.Hash(refreshToken);
        var now = UtcNow;
        var newExpiry = now.Add(RefreshTokenLifetime.Duration);

        var extendedCount = await _context.RefreshToken
            .Where(rt => rt.AspNetUsersId == userId && rt.Token == hashedToken && rt.ExpiryDate > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.ExpiryDate, newExpiry));

        return extendedCount > 0;
    }

    public async Task<string?> ReplaceRefreshTokenAsync(string userId, string legacyToken)
    {
        if (string.IsNullOrEmpty(legacyToken))
        {
            return null;
        }

        var hashedToken = RefreshTokenHasher.Hash(legacyToken);
        var now = UtcNow;
        var graceEnd = now.Add(RefreshTokenLifetime.LegacyTokenGrace);

        var shortenedCount = await _context.RefreshToken
            .Where(rt => rt.AspNetUsersId == userId && rt.Token == hashedToken && rt.ExpiryDate > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                rt => rt.ExpiryDate,
                rt => rt.ExpiryDate < graceEnd ? rt.ExpiryDate : graceEnd));

        return shortenedCount > 0 ? await CreateRefreshTokenAsync(userId) : null;
    }

    private async Task<List<RefreshToken>> LoadUserTokensAsync(string userId)
    {
        return await _context.RefreshToken
            .Where(rt => rt.AspNetUsersId == userId)
            .ToListAsync();
    }

    private (RefreshToken Entity, string RawToken) BuildRefreshToken(string userId)
    {
        var rawToken = new RefreshTokenGenerator().GenerateRefreshToken();
        var entity = new RefreshToken
        {
            AspNetUsersId = userId,
            Token = RefreshTokenHasher.Hash(rawToken),
            ExpiryDate = UtcNow.Add(RefreshTokenLifetime.Duration),
        };

        return (entity, rawToken);
    }

    private IEnumerable<RefreshToken> ExpiredAndOverflow(IEnumerable<RefreshToken> tokens)
    {
        var now = UtcNow;
        var all = tokens.ToList();
        var expired = all.Where(rt => rt.ExpiryDate <= now).ToList();

        // ExpiryDate slides to now + RefreshTokenLifetime on every refresh, so it orders
        // the sessions by last use: the overflow drops the least recently used ones.
        // Keep room for the one token about to be added.
        var overflow = all
            .Except(expired)
            .OrderByDescending(rt => rt.ExpiryDate)
            .Skip(Math.Max(0, MaxRefreshTokensPerUser - 1))
            .ToList();

        return expired.Concat(overflow);
    }

    public async Task<AppUser?> GetUserFromRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var hashedToken = RefreshTokenHasher.Hash(refreshToken);
            var storedRefreshToken = await _context.RefreshToken
                .Where(x => x.Token == hashedToken && x.ExpiryDate > UtcNow)
                .OrderByDescending(x => x.ExpiryDate)
                .FirstOrDefaultAsync();

            if (storedRefreshToken != null)
            {
                var user = await _context.AppUser
                    .FirstOrDefaultAsync(u => u.Id == storedRefreshToken.AspNetUsersId);
                return user;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user from refresh token");
        }

        return null;
    }

    public async Task RemoveAllUserRefreshTokensAsync(string userId)
    {
        var userTokens = await _context.RefreshToken.Where(rt => rt.AspNetUsersId == userId).ToListAsync();
        _context.RefreshToken.RemoveRange(userTokens);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return;
        }

        var hashedToken = RefreshTokenHasher.Hash(refreshToken);
        await _context.RefreshToken
            .Where(rt => rt.Token == hashedToken)
            .ExecuteDeleteAsync();
    }

    public DateTime CalculateTokenExpiryTime()
    {
        return UtcNow.Add(AccessTokenLifetime.Duration);
    }
}
