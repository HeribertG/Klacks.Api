// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Security;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Authentication;

public class RefreshTokenService : IRefreshTokenService
{
    private const int RefreshTokenLifetimeDays = 30;
    private const int MaxRefreshTokensPerUser = 20;

    private readonly DataBaseContext _context;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(DataBaseContext context, ILogger<RefreshTokenService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string> CreateRefreshTokenAsync(string userId)
    {
        var tokens = await LoadUserTokensAsync(userId);
        _context.RefreshToken.RemoveRange(ExpiredAndOverflow(tokens));

        var (entity, rawToken) = BuildRefreshToken(userId);
        _context.RefreshToken.Add(entity);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<string> RotateRefreshTokenAsync(string userId, string oldToken)
    {
        var hashedOldToken = RefreshTokenHasher.Hash(oldToken);
        var tokens = await LoadUserTokensAsync(userId);
        var consumed = tokens.Where(rt => rt.Token == hashedOldToken).ToList();
        if (consumed.Count == 0)
        {
            // Defense-in-depth: never issue a token for an unknown old token. The
            // caller validates first, so this only guards against future misuse.
            throw new InvalidOperationException("Refresh token not found for rotation.");
        }

        var survivors = tokens.Except(consumed);

        _context.RefreshToken.RemoveRange(consumed.Concat(ExpiredAndOverflow(survivors)));

        var (entity, rawToken) = BuildRefreshToken(userId);
        _context.RefreshToken.Add(entity);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    private async Task<List<RefreshToken>> LoadUserTokensAsync(string userId)
    {
        return await _context.RefreshToken
            .Where(rt => rt.AspNetUsersId == userId)
            .ToListAsync();
    }

    private static (RefreshToken Entity, string RawToken) BuildRefreshToken(string userId)
    {
        var rawToken = new RefreshTokenGenerator().GenerateRefreshToken();
        var entity = new RefreshToken
        {
            AspNetUsersId = userId,
            Token = RefreshTokenHasher.Hash(rawToken),
            ExpiryDate = DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays),
        };

        return (entity, rawToken);
    }

    private static IEnumerable<RefreshToken> ExpiredAndOverflow(IEnumerable<RefreshToken> tokens)
    {
        var now = DateTime.UtcNow;
        var all = tokens.ToList();
        var expired = all.Where(rt => rt.ExpiryDate <= now).ToList();

        // ExpiryDate doubles as creation order because every token is issued with a
        // fixed RefreshTokenLifetimeDays window, so the latest ExpiryDate is the
        // newest token. Keep room for the one token about to be added.
        var overflow = all
            .Except(expired)
            .OrderByDescending(rt => rt.ExpiryDate)
            .Skip(Math.Max(0, MaxRefreshTokensPerUser - 1))
            .ToList();

        return expired.Concat(overflow);
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        var hashedToken = RefreshTokenHasher.Hash(refreshToken);
        var storedRefreshToken = await _context.RefreshToken
            .Where(x => x.Token == hashedToken && x.AspNetUsersId == userId)
            .OrderByDescending(x => x.ExpiryDate)
            .FirstOrDefaultAsync();

        return storedRefreshToken?.ExpiryDate > DateTime.UtcNow;
    }

    public async Task<AppUser?> GetUserFromRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var hashedToken = RefreshTokenHasher.Hash(refreshToken);
            var storedRefreshToken = await _context.RefreshToken
                .Where(x => x.Token == hashedToken && x.ExpiryDate > DateTime.UtcNow)
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
        var userTokens = _context.RefreshToken.Where(rt => rt.AspNetUsersId == userId);
        _context.RefreshToken.RemoveRange(userTokens);
        await _context.SaveChangesAsync();
    }

    public DateTime CalculateTokenExpiryTime()
    {
        return DateTime.UtcNow.AddMinutes(15);
    }

    public DateTime CalculateRefreshTokenExpiryTime()
    {
        return DateTime.UtcNow.AddDays(RefreshTokenLifetimeDays);
    }
}