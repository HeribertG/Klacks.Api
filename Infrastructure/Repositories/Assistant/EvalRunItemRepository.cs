// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for the per-item breakdown of eval runs, self-committing. The watermark is written
/// through tracked entities rather than ExecuteUpdate because the batches are tiny (at most the evidence
/// of one proposal) and the in-memory provider the unit tests use does not translate ExecuteUpdate.
/// The selection-miss query carries every condition of its caller, the item ids included, so the limit
/// can never be filled with rows the caller has to discard afterwards.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class EvalRunItemRepository : IEvalRunItemRepository
{
    private readonly DataBaseContext _context;

    public EvalRunItemRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IReadOnlyList<EvalRunItem> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return;
        }

        await _context.EvalRunItems.AddRangeAsync(items, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvalRunItem>> ListByRunAsync(
        Guid evalRunId, CancellationToken cancellationToken = default)
    {
        return await _context.EvalRunItems
            .AsNoTracking()
            .Where(item => item.EvalRunId == evalRunId)
            .OrderBy(item => item.ItemId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EvalRunItem>> ListUnconsumedSelectionMissesAsync(
        Guid evalRunId,
        IReadOnlyCollection<string> itemIds,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (itemIds.Count == 0)
        {
            return [];
        }

        var wanted = itemIds.ToList();

        return await _context.EvalRunItems
            .AsNoTracking()
            .Where(item => item.EvalRunId == evalRunId
                && item.RetrievalHit == true
                && item.SelectionHit == false
                && item.LearningConsumedAtUtc == null
                && item.ChosenTool != null
                && item.ChosenTool != string.Empty
                && wanted.Contains(item.ItemId))
            .OrderBy(item => item.ItemId)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkConsumedAsync(
        IReadOnlyList<Guid> ids, DateTime consumedAtUtc, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var rows = await _context.EvalRunItems
            .Where(item => ids.Contains(item.Id))
            .ToListAsync(cancellationToken);

        foreach (var row in rows)
        {
            row.LearningConsumedAtUtc = consumedAtUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
