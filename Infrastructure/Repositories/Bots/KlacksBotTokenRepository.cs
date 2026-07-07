// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for Klacks bot tokens with hash lookup, listing, revocation and
/// last-used tracking.
/// </summary>
using Klacks.Api.Domain.Interfaces.Bots;
using Klacks.Api.Domain.Models.Bots;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Bots;

public class KlacksBotTokenRepository : IKlacksBotTokenRepository
{
    private readonly DataBaseContext _context;

    public KlacksBotTokenRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<KlacksBotToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<KlacksBotToken>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<KlacksBotToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<KlacksBotToken>()
            .AsNoTracking()
            .OrderByDescending(t => t.CreateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(KlacksBotToken token, CancellationToken cancellationToken = default)
    {
        await _context.Set<KlacksBotToken>().AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<KlacksBotToken?> RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await _context.Set<KlacksBotToken>()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (token == null)
        {
            return null;
        }

        _context.Set<KlacksBotToken>().Remove(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default)
    {
        await _context.Set<KlacksBotToken>()
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastUsedAt, lastUsedAt), cancellationToken);
    }
}
