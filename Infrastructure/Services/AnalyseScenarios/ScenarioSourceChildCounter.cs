// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one child count behind the accept conflict check, read among the real rows and the rows of the given variants (keys).
/// The clone writes it as the snapshot and the accept compares against it, so both sides must count the same population.
/// A real source counts its live children through ParentId, so a piece cut from a sealed order or plannable copy (no
/// ParentId) is not counted there; only a piece cut from a piece is. A source that is a key shift counts the live rows of its
/// own key token linked to it through ParentId or OriginalId (never the row itself), because the cut of a key writes a first
/// level piece without ParentId and that cut must change the count.
/// </summary>
/// <param name="context">Database context of the caller</param>
/// <param name="sourceIds">Source shifts whose children are counted</param>
/// <param name="variantTokens">Tokens of the variants whose rows count next to the real rows</param>

using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal static class ScenarioSourceChildCounter
{
    internal static async Task<Dictionary<Guid, int>> CountAsync(
        DataBaseContext context, IReadOnlyCollection<Guid> sourceIds, IReadOnlyCollection<Guid> variantTokens, CancellationToken ct)
    {
        if (sourceIds.Count == 0)
        {
            return [];
        }

        var tokens = variantTokens.ToList();
        var keyTokenBySource = await LoadKeyTokensAsync(context, sourceIds, tokens, ct);
        var realSourceIds = sourceIds.Where(id => !keyTokenBySource.ContainsKey(id)).Distinct().ToList();

        var counts = await CountRealChildrenAsync(context, realSourceIds, tokens, ct);
        foreach (var (sourceId, count) in await CountKeyChildrenAsync(context, keyTokenBySource, ct))
        {
            counts[sourceId] = count;
        }

        return counts;
    }

    private static async Task<Dictionary<Guid, Guid>> LoadKeyTokensAsync(
        DataBaseContext context, IReadOnlyCollection<Guid> sourceIds, List<Guid> tokens, CancellationToken ct)
    {
        if (tokens.Count == 0)
        {
            return [];
        }

        var ids = sourceIds.Distinct().ToList();

        return await context.Shift.IgnoreQueryFilters()
            .Where(s => ids.Contains(s.Id) && s.AnalyseToken.HasValue && tokens.Contains(s.AnalyseToken.Value))
            .ToDictionaryAsync(s => s.Id, s => s.AnalyseToken!.Value, ct);
    }

    private static async Task<Dictionary<Guid, int>> CountRealChildrenAsync(
        DataBaseContext context, List<Guid> parentIds, List<Guid> tokens, CancellationToken ct)
    {
        if (parentIds.Count == 0)
        {
            return [];
        }

        return await context.Shift.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                && (s.AnalyseToken == null || tokens.Contains(s.AnalyseToken.Value))
                && s.ParentId.HasValue
                && parentIds.Contains(s.ParentId.Value))
            .GroupBy(s => s.ParentId!.Value)
            .Select(g => new { ParentId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ParentId, x => x.Count, ct);
    }

    private static async Task<Dictionary<Guid, int>> CountKeyChildrenAsync(
        DataBaseContext context, IReadOnlyDictionary<Guid, Guid> keyTokenBySource, CancellationToken ct)
    {
        if (keyTokenBySource.Count == 0)
        {
            return [];
        }

        var keyIds = keyTokenBySource.Keys.ToList();
        var keyTokens = keyTokenBySource.Values.Distinct().ToList();

        var rows = await context.Shift.IgnoreQueryFilters()
            .Where(s => !s.IsDeleted
                && s.AnalyseToken.HasValue
                && keyTokens.Contains(s.AnalyseToken.Value)
                && ((s.ParentId.HasValue && keyIds.Contains(s.ParentId.Value))
                    || (s.OriginalId.HasValue && keyIds.Contains(s.OriginalId.Value))))
            .Select(s => new { s.Id, Token = s.AnalyseToken!.Value, s.ParentId, s.OriginalId })
            .ToListAsync(ct);

        return keyTokenBySource
            .Select(pair => (
                SourceId: pair.Key,
                Count: rows.Count(row => row.Token == pair.Value
                    && row.Id != pair.Key
                    && (row.ParentId == pair.Key || row.OriginalId == pair.Key))))
            .Where(entry => entry.Count > 0)
            .ToDictionary(entry => entry.SourceId, entry => entry.Count);
    }
}
