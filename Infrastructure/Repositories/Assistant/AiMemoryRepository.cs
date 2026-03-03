// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AiMemoryRepository : IAiMemoryRepository
{
    private readonly DataBaseContext _context;

    public AiMemoryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<AiMemory>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiMemories
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AiMemory>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        return await _context.AiMemories
            .Where(m => !m.IsDeleted && m.Category == category)
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AiMemory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AiMemories
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);
    }

    public async Task<List<AiMemory>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerSearch = searchTerm.ToLower();
        return await _context.AiMemories
            .Where(m => !m.IsDeleted &&
                        (m.Key.ToLower().Contains(lowerSearch) ||
                         m.Content.ToLower().Contains(lowerSearch)))
            .OrderByDescending(m => m.Importance)
            .ThenByDescending(m => m.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(AiMemory memory, CancellationToken cancellationToken = default)
    {
        await _context.AiMemories.AddAsync(memory, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiMemory memory, CancellationToken cancellationToken = default)
    {
        _context.AiMemories.Update(memory);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var memory = await _context.AiMemories
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        if (memory != null)
        {
            memory.IsDeleted = true;
            memory.DeletedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
