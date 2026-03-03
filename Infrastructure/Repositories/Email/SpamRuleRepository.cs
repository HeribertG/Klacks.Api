// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Email;

public class SpamRuleRepository : ISpamRuleRepository
{
    private readonly DataBaseContext _context;
    private readonly ILogger<SpamRuleRepository> _logger;

    public SpamRuleRepository(DataBaseContext context, ILogger<SpamRuleRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SpamRule>> GetAllActiveAsync()
    {
        return await _context.SpamRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<SpamRule>> GetAllAsync()
    {
        return await _context.SpamRules
            .OrderBy(r => r.SortOrder)
            .ToListAsync();
    }

    public async Task<SpamRule?> GetByIdAsync(Guid id)
    {
        return await _context.SpamRules.FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task AddAsync(SpamRule rule)
    {
        await _context.SpamRules.AddAsync(rule);
    }

    public Task UpdateAsync(SpamRule rule)
    {
        _context.SpamRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id)
    {
        var rule = await _context.SpamRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule != null)
        {
            _context.SpamRules.Remove(rule);
        }
    }
}
