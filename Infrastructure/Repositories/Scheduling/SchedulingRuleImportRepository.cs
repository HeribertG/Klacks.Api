// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ISchedulingRuleImportRepository"/>.
/// </summary>
/// <param name="context">Database context providing the SchedulingRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class SchedulingRuleImportRepository : ISchedulingRuleImportRepository
{
    private readonly DataBaseContext _context;

    public SchedulingRuleImportRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<SchedulingRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.SchedulingRules
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public void Add(SchedulingRule rule)
    {
        _context.SchedulingRules.Add(rule);
    }

    public void Update(SchedulingRule rule)
    {
        _context.SchedulingRules.Update(rule);
    }
}
