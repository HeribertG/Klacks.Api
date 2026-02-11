using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.AI;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.AI;

public class LlmFunctionDefinitionRepository : ILlmFunctionDefinitionRepository
{
    private readonly DataBaseContext _context;

    public LlmFunctionDefinitionRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<LlmFunctionDefinition>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.LlmFunctionDefinitions
            .Where(f => !f.IsDeleted && f.IsEnabled)
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<LlmFunctionDefinition?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.LlmFunctionDefinitions
            .FirstOrDefaultAsync(f => f.Name == name && !f.IsDeleted, cancellationToken);
    }

    public async Task<LlmFunctionDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LlmFunctionDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
    }

    public async Task AddAsync(LlmFunctionDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.LlmFunctionDefinitions.AddAsync(definition, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(LlmFunctionDefinition definition, CancellationToken cancellationToken = default)
    {
        _context.LlmFunctionDefinitions.Update(definition);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var definition = await _context.LlmFunctionDefinitions
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (definition != null)
        {
            definition.IsDeleted = true;
            definition.DeletedTime = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
