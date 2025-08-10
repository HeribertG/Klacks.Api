using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Security;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Authentication;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly DataBaseContext _context;

    public RefreshTokenService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<string> CreateRefreshTokenAsync(string userId)
    {
        await RemoveAllUserRefreshTokensAsync(userId);

        var refreshToken = new RefreshToken
        {
            AspNetUsersId = userId,
            Token = new RefreshTokenGenerator().GenerateRefreshToken(),
            ExpiryDate = DateTime.UtcNow.AddHours(1),
        };

        _context.RefreshToken.Add(refreshToken);
        await _context.SaveChangesAsync();

        return refreshToken.Token;
    }

    public async Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken)
    {
        var storedRefreshToken = await _context.RefreshToken
            .Where(x => x.Token == refreshToken && x.AspNetUsersId == userId)
            .OrderByDescending(x => x.ExpiryDate)
            .FirstOrDefaultAsync();

        return storedRefreshToken?.ExpiryDate > DateTime.UtcNow;
    }

    public async Task<AppUser?> GetUserFromRefreshTokenAsync(string refreshToken)
    {
        try
        {
            var storedRefreshToken = await _context.RefreshToken
                .Where(x => x.Token == refreshToken && x.ExpiryDate > DateTime.UtcNow)
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
            Console.WriteLine($"GetUserFromRefreshToken Error: {ex.Message}");
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
        return DateTime.UtcNow.AddHours(1);
    }
}