// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ISchedulingRuleRateRevisionImportRepository"/>.
/// </summary>
/// <param name="context">Database context providing the SchedulingRuleRateRevision DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class SchedulingRuleRateRevisionImportRepository : ISchedulingRuleRateRevisionImportRepository
{
    private readonly DataBaseContext _context;

    public SchedulingRuleRateRevisionImportRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<SchedulingRuleRateRevision>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.SchedulingRuleRateRevisions
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public void Add(SchedulingRuleRateRevision revision)
    {
        _context.SchedulingRuleRateRevisions.Add(revision);
    }

    public void Update(SchedulingRuleRateRevision revision)
    {
        _context.SchedulingRuleRateRevisions.Update(revision);
    }
}
