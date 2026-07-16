// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ICounterRuleRepository"/>.
/// </summary>
/// <param name="context">Database context providing the CounterRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class CounterRuleRepository : ICounterRuleRepository
{
    private readonly DataBaseContext _context;

    public CounterRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<CounterRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.CounterRule
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public async Task<List<CounterRule>> GetAllActiveAsync()
    {
        return await _context.CounterRule
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<CounterRule?> GetAsync(Guid id)
    {
        return await _context.CounterRule
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<CounterRule?> DeleteAsync(Guid id)
    {
        var entity = await _context.CounterRule
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (entity is null)
        {
            return null;
        }

        _context.Remove(entity);
        return entity;
    }

    public void Add(CounterRule rule)
    {
        _context.CounterRule.Add(rule);
    }

    public void Update(CounterRule rule)
    {
        _context.CounterRule.Update(rule);
    }
}
