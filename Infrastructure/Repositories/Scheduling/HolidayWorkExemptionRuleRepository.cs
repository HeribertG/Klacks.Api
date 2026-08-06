// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IHolidayWorkExemptionRuleRepository"/>.
/// </summary>
/// <param name="context">Database context providing the HolidayWorkExemptionRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class HolidayWorkExemptionRuleRepository : IHolidayWorkExemptionRuleRepository
{
    private readonly DataBaseContext _context;

    public HolidayWorkExemptionRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<HolidayWorkExemptionRule>> GetAllActiveAsync()
    {
        return await _context.HolidayWorkExemptionRule
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<HolidayWorkExemptionRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys)
    {
        if (sourceKeys.Count == 0)
        {
            return [];
        }

        return await _context.HolidayWorkExemptionRule
            .Where(r => !r.IsDeleted && sourceKeys.Contains(r.ImportSourceKey))
            .ToListAsync();
    }

    public async Task<HolidayWorkExemptionRule?> GetAsync(Guid id)
    {
        return await _context.HolidayWorkExemptionRule.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public void Add(HolidayWorkExemptionRule rule) => _context.HolidayWorkExemptionRule.Add(rule);

    public void Update(HolidayWorkExemptionRule rule) => _context.HolidayWorkExemptionRule.Update(rule);

    public async Task<HolidayWorkExemptionRule?> DeleteAsync(Guid id)
    {
        var rule = await GetAsync(id);
        if (rule == null)
        {
            return null;
        }

        _context.HolidayWorkExemptionRule.Remove(rule);
        return rule;
    }
}
