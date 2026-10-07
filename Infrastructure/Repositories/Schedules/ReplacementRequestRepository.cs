// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core repository of the replacement request book; stage-only except the bulk retention delete.
/// </summary>
/// <param name="context">Shared database context of the request scope</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ReplacementRequestRepository : BaseRepository<ReplacementRequest>, IReplacementRequestRepository
{
    public ReplacementRequestRepository(DataBaseContext context, ILogger<ReplacementRequest> logger)
        : base(context, logger)
    {
    }

    public async Task<ReplacementRequest?> FindLiveAsync(
        Guid? analyseToken, Guid candidateClientId, Guid shiftId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var staged = context.ReplacementRequests.Local.FirstOrDefault(r =>
            !r.IsDeleted
            && r.AnalyseToken == analyseToken
            && r.CandidateClientId == candidateClientId
            && r.ShiftId == shiftId
            && r.Date == date);
        if (staged is not null)
        {
            return staged;
        }

        return await context.ReplacementRequests
            .Where(r => r.AnalyseToken == analyseToken
                && r.CandidateClientId == candidateClientId
                && r.ShiftId == shiftId
                && r.Date == date)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ReplacementRequest>> ListLiveByTokenAsync(Guid analyseToken, CancellationToken cancellationToken = default)
    {
        await context.ReplacementRequests
            .Where(r => r.AnalyseToken == analyseToken)
            .LoadAsync(cancellationToken);

        return context.ReplacementRequests.Local
            .Where(r => !r.IsDeleted && r.AnalyseToken == analyseToken)
            .ToList();
    }

    public async Task<ReplacementRequest?> FindManualByWorkChangeIdAsync(Guid workChangeId, CancellationToken cancellationToken = default)
    {
        var staged = context.ReplacementRequests.Local.FirstOrDefault(r =>
            !r.IsDeleted && r.WorkChangeId == workChangeId && r.Source == ReplacementRequestSource.ManualReplacement);
        if (staged is not null)
        {
            return staged;
        }

        return await context.ReplacementRequests
            .Where(r => r.WorkChangeId == workChangeId && r.Source == ReplacementRequestSource.ManualReplacement)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<DateTime?> FindReportedAtAsync(Guid analyseToken, Guid absentClientId, CancellationToken cancellationToken = default)
    {
        return await context.ReplacementRequests
            .Where(r => r.AnalyseToken == analyseToken && r.AbsentClientId == absentClientId)
            .OrderBy(r => r.ReportedAtUtc)
            .Select(r => (DateTime?)r.ReportedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<ReplacementRequest>> ListAsync(
        ReplacementRequestFilter filter, int maxRows, CancellationToken cancellationToken = default)
    {
        var query = context.ReplacementRequests.AsNoTracking();

        if (filter.AbsentClientId.HasValue)
        {
            query = query.Where(r => r.AbsentClientId == filter.AbsentClientId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(r => r.Date >= filter.FromDate.Value);
        }

        if (filter.UntilDate.HasValue)
        {
            query = query.Where(r => r.Date <= filter.UntilDate.Value);
        }

        if (filter.AnalyseToken.HasValue)
        {
            query = query.Where(r => r.AnalyseToken == filter.AnalyseToken.Value);
        }

        return await query
            .OrderByDescending(r => r.Date)
            .ThenBy(r => r.StartTime)
            .ThenBy(r => r.Id)
            .Take(maxRows)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteReportedBeforeAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await context.ReplacementRequests
            .IgnoreQueryFilters()
            .Where(r => r.ReportedAtUtc < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
