// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class SchedulingRuleRepository : BaseRepository<SchedulingRule>, ISchedulingRuleRepository
{
    public SchedulingRuleRepository(DataBaseContext context, ILogger<SchedulingRule> logger)
        : base(context, logger)
    {
    }

    public async Task<List<SchedulingRule>> GetSelectableAsync(IReadOnlyCollection<string> activeIndustrySlugs)
    {
        if (activeIndustrySlugs.Count == 0)
        {
            return await context.SchedulingRules
                .AsNoTracking()
                .Where(rule => rule.Industry == string.Empty)
                .ToListAsync();
        }

        var slugs = activeIndustrySlugs.Select(slug => slug.ToLowerInvariant()).ToList();
        return await context.SchedulingRules
            .AsNoTracking()
            .Where(rule => rule.Industry == string.Empty || slugs.Contains(rule.Industry))
            .ToListAsync();
    }

    public async Task<List<SchedulingRule>> GetByIndustryAsync(string industry)
    {
        var slug = industry.ToLowerInvariant();
        return await context.SchedulingRules
            .AsNoTracking()
            .Where(rule => rule.Industry == slug)
            .ToListAsync();
    }

    public async Task<int> GetCustomRuleCountAsync()
    {
        return await context.SchedulingRules
            .AsNoTracking()
            .CountAsync(rule => rule.Industry == string.Empty);
    }
}
