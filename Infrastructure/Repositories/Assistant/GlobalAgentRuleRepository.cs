// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class GlobalAgentRuleRepository : IGlobalAgentRuleRepository
{
    private readonly DataBaseContext _context;

    public GlobalAgentRuleRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<GlobalAgentRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GlobalAgentRules
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<GlobalAgentRule?> GetRuleAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.GlobalAgentRules
            .FirstOrDefaultAsync(r => r.Name == name && r.IsActive, cancellationToken);
    }

    public async Task<GlobalAgentRule> UpsertRuleAsync(
        string name, string content, int sortOrder,
        string? source = null, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GlobalAgentRules
            .FirstOrDefaultAsync(r => r.Name == name && r.IsActive, cancellationToken);

        if (existing != null)
        {
            var contentBefore = existing.Content;
            existing.Content = content;
            existing.SortOrder = sortOrder;
            existing.Source = source;
            existing.Version++;

            _context.GlobalAgentRuleHistories.Add(new GlobalAgentRuleHistory
            {
                GlobalAgentRuleId = existing.Id,
                Name = name,
                ContentBefore = contentBefore,
                ContentAfter = content,
                Version = existing.Version,
                ChangeType = SoulChangeTypes.Update,
                ChangedBy = changedBy
            });

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var newRule = new GlobalAgentRule
        {
            Name = name,
            Content = content,
            SortOrder = sortOrder,
            IsActive = true,
            Version = 1,
            Source = source
        };

        _context.GlobalAgentRules.Add(newRule);

        _context.GlobalAgentRuleHistories.Add(new GlobalAgentRuleHistory
        {
            GlobalAgentRuleId = newRule.Id,
            Name = name,
            ContentBefore = null,
            ContentAfter = content,
            Version = 1,
            ChangeType = SoulChangeTypes.Create,
            ChangedBy = changedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return newRule;
    }

    public async Task DeactivateRuleAsync(string name, CancellationToken cancellationToken = default)
    {
        var rule = await _context.GlobalAgentRules
            .FirstOrDefaultAsync(r => r.Name == name && r.IsActive, cancellationToken);

        if (rule != null)
        {
            rule.IsActive = false;

            _context.GlobalAgentRuleHistories.Add(new GlobalAgentRuleHistory
            {
                GlobalAgentRuleId = rule.Id,
                Name = name,
                ContentBefore = rule.Content,
                ContentAfter = rule.Content,
                Version = rule.Version,
                ChangeType = SoulChangeTypes.Deactivate
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<GlobalAgentRuleHistory>> GetHistoryAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.GlobalAgentRuleHistories
            .OrderByDescending(h => h.CreateTime)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
