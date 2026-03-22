// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for SkillSynonym entities.
/// </summary>
/// <param name="context">The database context for accessing the skill_synonyms table</param>
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillSynonymRepository : ISkillSynonymRepository
{
    private readonly DataBaseContext _context;

    public SkillSynonymRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SkillSynonym>> GetByLanguageAsync(string language, CancellationToken ct = default)
    {
        return await _context.SkillSynonyms
            .Where(s => s.Language == language && !s.IsDeleted)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task AddRangeAsync(IEnumerable<SkillSynonym> synonyms, CancellationToken ct = default)
    {
        await _context.SkillSynonyms.AddRangeAsync(synonyms, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAllAsync(CancellationToken ct = default)
    {
        var all = await _context.SkillSynonyms
            .IgnoreQueryFilters()
            .ToListAsync(ct);

        _context.SkillSynonyms.RemoveRange(all);
        await _context.SaveChangesAsync(ct);
    }
}
