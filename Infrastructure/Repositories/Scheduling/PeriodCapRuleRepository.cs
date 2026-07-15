// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IPeriodCapRuleRepository"/>.
/// </summary>
/// <param name="context">Database context providing the PeriodCapRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class PeriodCapRuleRepository : IPeriodCapRuleRepository
{
    private readonly DataBaseContext _context;

    public PeriodCapRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<PeriodCapRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.PeriodCapRule
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public async Task<List<PeriodCapRule>> GetAllActiveAsync()
    {
        return await _context.PeriodCapRule
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public void Add(PeriodCapRule rule)
    {
        _context.PeriodCapRule.Add(rule);
    }

    public void Update(PeriodCapRule rule)
    {
        _context.PeriodCapRule.Update(rule);
    }
}
