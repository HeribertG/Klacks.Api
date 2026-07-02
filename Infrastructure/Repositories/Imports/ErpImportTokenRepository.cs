// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for ERP import tokens with hash lookup, per-drop-point listing,
/// drop-point-scoped revocation and last-used tracking.
/// </summary>
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Imports;

public class ErpImportTokenRepository : IErpImportTokenRepository
{
    private readonly DataBaseContext _context;

    public ErpImportTokenRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<ErpImportToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportToken>()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<List<ErpImportToken>> GetByDropPointAsync(Guid dropPointId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ErpImportToken>()
            .AsNoTracking()
            .Where(t => t.DropPointId == dropPointId)
            .OrderByDescending(t => t.CreateTime)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ErpImportToken token, CancellationToken cancellationToken = default)
    {
        await _context.Set<ErpImportToken>().AddAsync(token, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ErpImportToken?> RevokeAsync(Guid id, Guid dropPointId, CancellationToken cancellationToken = default)
    {
        var token = await _context.Set<ErpImportToken>()
            .FirstOrDefaultAsync(t => t.Id == id && t.DropPointId == dropPointId, cancellationToken);

        if (token == null)
        {
            return null;
        }

        _context.Set<ErpImportToken>().Remove(token);
        await _context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default)
    {
        await _context.Set<ErpImportToken>()
            .Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastUsedAt, lastUsedAt), cancellationToken);
    }
}
