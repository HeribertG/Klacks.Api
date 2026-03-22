// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for reading and writing multilingual sentiment keyword sets in the database.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SentimentKeywordRepository : ISentimentKeywordRepository
{
    private readonly DataBaseContext _context;

    public SentimentKeywordRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<List<SentimentKeywordSet>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SentimentKeywordSets
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(string language, Dictionary<string, List<string>> keywords, string source, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SentimentKeywordSets
            .FirstOrDefaultAsync(s => s.Language == language, cancellationToken);

        if (existing != null)
        {
            existing.Keywords = keywords;
            existing.Source = source;
            existing.UpdateTime = DateTime.UtcNow;
        }
        else
        {
            _context.SentimentKeywordSets.Add(new SentimentKeywordSet
            {
                Id = Guid.NewGuid(),
                Language = language,
                Keywords = keywords,
                Source = source,
                CreateTime = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteByLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        var existing = await _context.SentimentKeywordSets
            .FirstOrDefaultAsync(s => s.Language == language, cancellationToken);

        if (existing == null)
            return;

        _context.SentimentKeywordSets.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
