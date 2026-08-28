// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the frozen routing expectations, self-committing.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillLearningGoldenCaseRepository : ISkillLearningGoldenCaseRepository
{
    private readonly DataBaseContext _context;

    public SkillLearningGoldenCaseRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillLearningGoldenCase goldenCase, CancellationToken cancellationToken = default)
    {
        await _context.SkillLearningGoldenCases.AddAsync(goldenCase, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SkillLearningGoldenCase>> ListAsync(
        int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .OrderByDescending(c => c.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string query, string expectedSourceId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillLearningGoldenCases
            .AsNoTracking()
            .AnyAsync(c => c.Query == query && c.ExpectedSourceId == expectedSourceId, cancellationToken);
    }
}
