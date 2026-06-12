// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for personal access tokens with hash lookup, per-user listing,
/// owner-scoped soft-delete revocation and last-used tracking.
/// </summary>
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class PersonalAccessTokenRepository : IPersonalAccessTokenRepository
{
    private readonly DataBaseContext _context;

    public PersonalAccessTokenRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<PersonalAccessToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.PersonalAccessTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<PersonalAccessToken>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.PersonalAccessTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default)
    {
        await _context.PersonalAccessTokens.AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PersonalAccessToken?> RevokeAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var token = await _context.PersonalAccessTokens
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

        if (token == null)
        {
            return null;
        }

        _context.PersonalAccessTokens.Remove(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default)
    {
        await _context.PersonalAccessTokens
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastUsedAt, lastUsedAt), cancellationToken);
    }
}
