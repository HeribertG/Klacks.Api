// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IRestDayRotationRuleRepository"/>.
/// </summary>
/// <param name="context">Database context providing the RestDayRotationRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class RestDayRotationRuleRepository : IRestDayRotationRuleRepository
{
    private readonly DataBaseContext _context;

    public RestDayRotationRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<RestDayRotationRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.RestDayRotationRule
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public async Task<List<RestDayRotationRule>> GetAllActiveAsync()
    {
        return await _context.RestDayRotationRule
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public void Add(RestDayRotationRule rule)
    {
        _context.RestDayRotationRule.Add(rule);
    }

    public void Update(RestDayRotationRule rule)
    {
        _context.RestDayRotationRule.Update(rule);
    }
}
