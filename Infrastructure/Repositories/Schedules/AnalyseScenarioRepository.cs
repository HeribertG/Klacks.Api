// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository implementation for AnalyseScenario with group and token queries.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
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

    public async Task<List<AnalyseScenario>> ListVisibleAsync(
        Guid? groupId, bool onlyOpen, IReadOnlyCollection<Guid>? visibleRootIds, CancellationToken ct = default)
    {
        var query = context.Set<AnalyseScenario>()
            .Include(s => s.Group)
            .Where(s => !s.IsDeleted);

        if (groupId.HasValue)
        {
            query = query.Where(s => s.GroupId == groupId);
        }

        if (onlyOpen)
        {
            query = query.Where(s => s.Status == AnalyseScenarioStatus.Active);
        }

        if (visibleRootIds != null)
        {
            var rootIds = visibleRootIds.ToList();
            query = query.Where(s => s.Group != null && rootIds.Contains(s.Group.Root ?? s.Group.Id));
        }

        return await query
            .OrderByDescending(s => s.CreateTime)
            .ToListAsync(ct);
    }

    public async Task<List<AnalyseScenario>> GetActiveCreatedBetweenAsync(
        DateTime createdAfterUtc, DateTime createdBeforeUtc, CancellationToken ct = default)
    {
        return await context.Set<AnalyseScenario>()
            .Include(s => s.Group)
            .Where(s => !s.IsDeleted
                && s.Status == AnalyseScenarioStatus.Active
                && s.CreateTime != null
                && s.CreateTime > createdAfterUtc
                && s.CreateTime <= createdBeforeUtc)
            .OrderByDescending(s => s.CreateTime)
            .ToListAsync(ct);
    }

    public async Task<AnalyseScenario?> GetActiveCandidateAsync(
        string createdByUser, Guid? groupId, DateOnly fromDate, DateOnly untilDate, CancellationToken ct = default)
    {
        return await context.Set<AnalyseScenario>()
            .Where(s => !s.IsDeleted
                && s.Status == AnalyseScenarioStatus.Active
                && s.CreatedByUser == createdByUser
                && s.GroupId == groupId
                && s.FromDate == fromDate
                && s.UntilDate == untilDate)
            .OrderByDescending(s => s.CreateTime)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<AnalyseScenario>> GetStaleCandidatesAsync(
        string createdByUser, DateTime createdBeforeUtc, CancellationToken ct = default)
    {
        return await context.Set<AnalyseScenario>()
            .Where(s => !s.IsDeleted
                && s.Status == AnalyseScenarioStatus.Active
                && s.CreatedByUser == createdByUser
                && s.CreateTime != null
                && s.CreateTime < createdBeforeUtc)
            .OrderByDescending(s => s.CreateTime)
            .ToListAsync(ct);
    }

    public async Task<AnalyseScenario?> GetByTokenAsync(Guid token, CancellationToken ct = default)
    {
        return await context.Set<AnalyseScenario>()
            .FirstOrDefaultAsync(s => s.Token == token && !s.IsDeleted, ct);
    }
}
