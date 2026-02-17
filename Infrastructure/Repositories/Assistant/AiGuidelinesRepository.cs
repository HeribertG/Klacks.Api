using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AiGuidelinesRepository : IAiGuidelinesRepository
{
    private readonly DataBaseContext _context;

    public AiGuidelinesRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AiGuidelines?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiGuidelines
            .Where(g => !g.IsDeleted && g.IsActive)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AiGuidelines>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AiGuidelines
            .Where(g => !g.IsDeleted)
            .OrderByDescending(g => g.IsActive)
            .ThenByDescending(g => g.CreateTime)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AiGuidelines?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.AiGuidelines
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(AiGuidelines guidelines, CancellationToken cancellationToken = default)
    {
        await _context.AiGuidelines.AddAsync(guidelines, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AiGuidelines guidelines, CancellationToken cancellationToken = default)
    {
        _context.AiGuidelines.Update(guidelines);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAllAsync(CancellationToken cancellationToken = default)
    {
        var activeGuidelines = await _context.AiGuidelines
            .Where(g => !g.IsDeleted && g.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var guidelines in activeGuidelines)
        {
            guidelines.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
