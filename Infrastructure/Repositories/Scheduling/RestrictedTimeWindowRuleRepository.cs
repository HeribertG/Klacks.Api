// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IRestrictedTimeWindowRuleRepository"/>.
/// </summary>
/// <param name="context">Database context providing the RestrictedTimeWindowRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class RestrictedTimeWindowRuleRepository : IRestrictedTimeWindowRuleRepository
{
    private readonly DataBaseContext _context;

    public RestrictedTimeWindowRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<RestrictedTimeWindowRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.RestrictedTimeWindowRule
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public async Task<List<RestrictedTimeWindowRule>> GetAllActiveAsync()
    {
        return await _context.RestrictedTimeWindowRule
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<RestrictedTimeWindowRule?> GetAsync(Guid id)
    {
        return await _context.RestrictedTimeWindowRule
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public async Task<RestrictedTimeWindowRule?> DeleteAsync(Guid id)
    {
        var entity = await _context.RestrictedTimeWindowRule
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (entity is null)
        {
            return null;
        }

        _context.Remove(entity);
        return entity;
    }

    public void Add(RestrictedTimeWindowRule rule)
    {
        _context.RestrictedTimeWindowRule.Add(rule);
    }

    public void Update(RestrictedTimeWindowRule rule)
    {
        _context.RestrictedTimeWindowRule.Update(rule);
    }
}
