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
        var slugs = activeIndustrySlugs.Select(slug => slug.ToLowerInvariant()).ToList();
        return await context.SchedulingRules
            .Where(rule => rule.Industry == string.Empty || slugs.Contains(rule.Industry.ToLower()))
            .ToListAsync();
    }
}
