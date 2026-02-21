using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AgentSoulRepository : IAgentSoulRepository
{
    private readonly DataBaseContext _context;

    public AgentSoulRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<AgentSoulSection>> GetActiveSectionsAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSoulSections
            .Where(s => s.AgentId == agentId && s.IsActive)
            .OrderBy(s => s.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<AgentSoulSection?> GetSectionAsync(Guid agentId, string sectionType, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSoulSections
            .FirstOrDefaultAsync(s => s.AgentId == agentId && s.SectionType == sectionType && s.IsActive, cancellationToken);
    }

    public async Task<AgentSoulSection> UpsertSectionAsync(
        Guid agentId, string sectionType, string content, int sortOrder,
        string? source = null, string? changedBy = null, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AgentSoulSections
            .FirstOrDefaultAsync(s => s.AgentId == agentId && s.SectionType == sectionType && s.IsActive, cancellationToken);

        if (existing != null)
        {
            var contentBefore = existing.Content;
            existing.Content = content;
            existing.SortOrder = sortOrder;
            existing.Source = source;
            existing.Version++;

            _context.AgentSoulHistories.Add(new AgentSoulHistory
            {
                AgentId = agentId,
                SoulSectionId = existing.Id,
                SectionType = sectionType,
                ContentBefore = contentBefore,
                ContentAfter = content,
                Version = existing.Version,
                ChangeType = SoulChangeTypes.Update,
                ChangedBy = changedBy
            });

            await _context.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var newSection = new AgentSoulSection
        {
            AgentId = agentId,
            SectionType = sectionType,
            Content = content,
            SortOrder = sortOrder,
            IsActive = true,
            Version = 1,
            Source = source
        };

        _context.AgentSoulSections.Add(newSection);

        _context.AgentSoulHistories.Add(new AgentSoulHistory
        {
            AgentId = agentId,
            SoulSectionId = newSection.Id,
            SectionType = sectionType,
            ContentBefore = null,
            ContentAfter = content,
            Version = 1,
            ChangeType = SoulChangeTypes.Create,
            ChangedBy = changedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
        return newSection;
    }

    public async Task DeactivateSectionAsync(Guid agentId, string sectionType, CancellationToken cancellationToken = default)
    {
        var section = await _context.AgentSoulSections
            .FirstOrDefaultAsync(s => s.AgentId == agentId && s.SectionType == sectionType && s.IsActive, cancellationToken);

        if (section != null)
        {
            section.IsActive = false;

            _context.AgentSoulHistories.Add(new AgentSoulHistory
            {
                AgentId = agentId,
                SoulSectionId = section.Id,
                SectionType = sectionType,
                ContentBefore = section.Content,
                ContentAfter = section.Content,
                Version = section.Version,
                ChangeType = SoulChangeTypes.Deactivate
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<List<AgentSoulHistory>> GetHistoryAsync(Guid agentId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _context.AgentSoulHistories
            .Where(h => h.AgentId == agentId)
            .OrderByDescending(h => h.CreateTime)
            .Take(limit)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
