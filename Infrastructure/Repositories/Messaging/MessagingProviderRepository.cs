// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository implementation for messaging provider CRUD operations.
/// </summary>
using Klacks.Api.Domain.Interfaces.Messaging;
using Klacks.Api.Domain.Models.Messaging;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Messaging;

public class MessagingProviderRepository : IMessagingProviderRepository
{
    private readonly DataBaseContext _context;

    public MessagingProviderRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<MessagingProvider?> GetByIdAsync(Guid id)
    {
        return await _context.MessagingProviders.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<MessagingProvider?> GetByNameAsync(string name)
    {
        return await _context.MessagingProviders.FirstOrDefaultAsync(p => p.Name == name);
    }

    public async Task<IReadOnlyList<MessagingProvider>> GetAllAsync()
    {
        return await _context.MessagingProviders.AsNoTracking().ToListAsync();
    }

    public async Task<IReadOnlyList<MessagingProvider>> GetEnabledAsync()
    {
        return await _context.MessagingProviders
            .Where(p => p.IsEnabled)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(MessagingProvider provider)
    {
        await _context.MessagingProviders.AddAsync(provider);
    }

    public async Task DeleteAsync(Guid id)
    {
        var provider = await _context.MessagingProviders.FirstOrDefaultAsync(p => p.Id == id);
        if (provider != null)
        {
            _context.MessagingProviders.Remove(provider);
        }
    }
}
