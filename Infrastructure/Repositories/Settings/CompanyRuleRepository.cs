// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ICompanyRuleRepository"/> over the company_rule table. Soft-delete is applied by
/// the context on save, so DeleteAsync only marks the row via Remove; the query filter hides deleted
/// rows from the list and single-row reads.
/// </summary>
/// <param name="context">Database context providing the CompanyRule DbSet</param>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Settings;

public class CompanyRuleRepository : ICompanyRuleRepository
{
    private readonly DataBaseContext _context;

    public CompanyRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<CompanyRule>> GetAllActiveAsync()
    {
        return await _context.CompanyRule
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.CreateTime)
            .ToListAsync();
    }

    public async Task<CompanyRule?> GetAsync(Guid id)
    {
        return await _context.CompanyRule
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
    }

    public void Add(CompanyRule rule)
    {
        _context.CompanyRule.Add(rule);
    }

    public async Task<CompanyRule?> DeleteAsync(Guid id)
    {
        var entity = await _context.CompanyRule
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);

        if (entity is null)
        {
            return null;
        }

        _context.Remove(entity);
        return entity;
    }
}
