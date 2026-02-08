using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.AI;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.AI;

public class AiSoulRepository : IAiSoulRepository
{
    private readonly DataBaseContext _context;

    public AiSoulRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AiSoul?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiSouls
            .Where(s => !s.IsDeleted && s.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AiSoul>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiSouls
            .Where(s => !s.IsDeleted)
            .OrderByDescending(s => s.IsActive)
            .ThenByDescending(s => s.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AiSoul?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AiSouls
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(AiSoul soul, CancellationToken cancellationToken = default)
    {
        await _context.AiSouls.AddAsync(soul, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiSoul soul, CancellationToken cancellationToken = default)
    {
        _context.AiSouls.Update(soul);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAllAsync(CancellationToken cancellationToken = default)
    {
        var activeSouls = await _context.AiSouls
            .Where(s => !s.IsDeleted && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var soul in activeSouls)
        {
            soul.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
