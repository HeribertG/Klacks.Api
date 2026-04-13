// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for custom STT provider CRUD with soft-delete support and system-provider protection.
/// </summary>
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class CustomSttProviderRepository : ICustomSttProviderRepository
{
    private readonly DataBaseContext _context;

    public CustomSttProviderRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<CustomSttProvider>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CustomSttProviders
            .OrderByDescending(e => e.IsSystem)
            .ThenBy(e => e.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CustomSttProvider>> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CustomSttProviders
            .Where(e => e.IsEnabled)
            .OrderByDescending(e => e.IsSystem)
            .ThenBy(e => e.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomSttProvider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.CustomSttProviders
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task AddAsync(CustomSttProvider provider, CancellationToken cancellationToken = default)
    {
        await _context.CustomSttProviders.AddAsync(provider, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(CustomSttProvider provider, CancellationToken cancellationToken = default)
    {
        _context.CustomSttProviders.Update(provider);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var provider = await _context.CustomSttProviders
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (provider != null && !provider.IsSystem)
        {
            _context.CustomSttProviders.Remove(provider);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
