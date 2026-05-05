// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository implementation for AnalyseScenario with group and token queries.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class AnalyseScenarioRepository : BaseRepository<AnalyseScenario>, IAnalyseScenarioRepository
{
    public AnalyseScenarioRepository(DataBaseContext context, ILogger<AnalyseScenario> logger)
        : base(context, logger)
    {
    }

    public async Task<List<AnalyseScenario>> GetByGroupAsync(Guid? groupId, CancellationToken ct = default)
    {
        return await context.Set<AnalyseScenario>()
            .Where(s => !s.IsDeleted && s.GroupId == groupId)
            .OrderByDescending(s => s.CreateTime)
            .ToListAsync(ct);
    }

    public async Task<AnalyseScenario?> GetByTokenAsync(Guid token, CancellationToken ct = default)
    {
        return await context.Set<AnalyseScenario>()
            .FirstOrDefaultAsync(s => s.Token == token && !s.IsDeleted, ct);
    }
}
