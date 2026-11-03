// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for WorkChange entries; runs the surcharge macro on add (after the entity is tracked, so the effective
/// window sees it) and on put.
/// </summary>
/// <param name="workMacroService">Computes surcharges and surcharge items of a WorkChange via the shift macro</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services.AnalyseScenarios;
using Klacks.Api.Infrastructure.Services.Schedules;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class WorkChangeRepository : BaseRepository<WorkChange>, IWorkChangeRepository
{
    private readonly DataBaseContext _context;
    private readonly IWorkMacroService _workMacroService;

    public WorkChangeRepository(
        DataBaseContext context,
        ILogger<WorkChange> logger,
        IWorkMacroService workMacroService)
        : base(context, logger)
    {
        _context = context;
        _workMacroService = workMacroService;
    }

    public override async Task Add(WorkChange entity)
    {
        await base.Add(entity);
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
    }

    public override async Task<WorkChange?> Put(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        return await base.Put(entity);
    }

    public override async Task<WorkChange?> Get(Guid id)
    {
        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .FirstOrDefaultAsync(wc => wc.Id == id && wc.AnalyseToken == null);
    }

    public async Task<WorkChange?> GetWithWorkInAnyScope(Guid id)
    {
        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .FirstOrDefaultAsync(wc => wc.Id == id);
    }

    public async Task<List<WorkChange>> ListScenarioReplacementsAsync(
        Guid analyseToken, Guid clientId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var replacementTypes = ReplacementWorkChangeTypes.All.ToList();

        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .Where(wc => wc.Work!.AnalyseToken == analyseToken
                && wc.Work.ClientId == clientId
                && wc.Work.CurrentDate == date
                && wc.Work.ParentWorkId == null
                && wc.ReplaceClientId != null
                && replacementTypes.Contains(wc.Type))
            .OrderBy(wc => wc.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkChange>> ListReplacementsWrittenByScenarioAsync(
        Guid analyseToken, Guid clientId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var replacementTypes = ReplacementWorkChangeTypes.All.ToList();

        var changes = await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .Where(wc => wc.Work!.AnalyseToken == analyseToken
                && wc.Work.ClientId == clientId
                && wc.Work.CurrentDate == date
                && wc.Work.ParentWorkId == null
                && wc.ReplaceClientId != null
                && replacementTypes.Contains(wc.Type))
            .OrderBy(wc => wc.Id)
            .ToListAsync(cancellationToken);

        var origins = await WorkCloneLineage.LoadRealOriginsAsync(
            _context,
            changes.Select(wc => wc.Work!).DistinctBy(work => work.Id).Select(work => (work.Id, work.ClonedFromWorkId)),
            cancellationToken);
        var realWorkIds = origins.Values.Select(origin => origin.WorkId).Distinct().ToList();
        var settled = await _context.WorkChange.IgnoreQueryFilters().AsNoTracking()
            .Where(wc => realWorkIds.Contains(wc.WorkId) && wc.ReplaceClientId != null)
            .Select(wc => new { wc.WorkId, wc.Type, wc.ReplaceClientId, wc.Description })
            .ToListAsync(cancellationToken);

        return changes
            .Where(wc => !origins.TryGetValue(wc.WorkId, out var origin)
                || !settled.Any(real => real.WorkId == origin.WorkId
                    && real.Type == wc.Type
                    && real.ReplaceClientId == wc.ReplaceClientId
                    && real.Description == wc.Description))
            .ToList();
    }

    public async Task<List<WorkChange>> ListNonReplacementChangesAsync(
        Guid workId, CancellationToken cancellationToken = default)
    {
        var replacementTypes = ReplacementWorkChangeTypes.All.ToList();

        return await _context.WorkChange
            .AsNoTracking()
            .Where(wc => wc.WorkId == workId && !replacementTypes.Contains(wc.Type))
            .OrderBy(wc => wc.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingScenarioReplacementAsync(
        Guid analyseToken,
        Guid clientId,
        DateOnly date,
        TimeOnly workStart,
        TimeOnly start,
        TimeOnly end,
        CancellationToken cancellationToken = default)
    {
        var covers = await ReplacementCoverQuery.LoadAsync(
            _context, analyseToken, date.AddDays(-1), date.AddDays(1), [clientId], cancellationToken);
        var (slotStart, slotEnd) = ReplacementWindow.ToInterval(date, workStart, start, end);

        return covers.Any(cover => cover.SubstituteClientId == clientId && cover.StartAt < slotEnd && slotStart < cover.EndAt);
    }

    public async Task<bool> IsReplacementInScenarioAsync(
        Guid analyseToken, Guid clientId, CancellationToken cancellationToken = default)
    {
        var replacementTypes = ReplacementWorkChangeTypes.All.ToList();

        return await _context.WorkChange
            .AsNoTracking()
            .AnyAsync(
                wc => wc.Work!.AnalyseToken == analyseToken
                    && wc.ReplaceClientId == clientId
                    && replacementTypes.Contains(wc.Type),
                cancellationToken);
    }
}
