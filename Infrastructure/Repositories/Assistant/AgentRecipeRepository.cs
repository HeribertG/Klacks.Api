// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads and writes operator-authored recipes from the agent_recipe table. Enabled recipes are
/// returned ordered by sort order and name for the deterministic forcing resolver.
/// </summary>
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentRecipeRepository : IAgentRecipeRepository
{
    private readonly DataBaseContext _context;

    public AgentRecipeRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<AgentRecipe>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AgentRecipes
            .Where(r => r.IsEnabled && !r.IsDeleted)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AgentRecipe>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.AgentRecipes
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentRecipe?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.AgentRecipes
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    public async Task AddAsync(AgentRecipe recipe, CancellationToken cancellationToken = default)
    {
        await _context.AgentRecipes.AddAsync(recipe, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AgentRecipe recipe, CancellationToken cancellationToken = default)
    {
        _context.AgentRecipes.Update(recipe);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var recipe = await _context.AgentRecipes
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recipe != null)
        {
            _context.AgentRecipes.Remove(recipe);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
