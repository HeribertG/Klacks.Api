// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for variants (keys) and their scenario links. Reads filter kind Variant and soft-deleted rows on every
/// side of a join; writes only stage changes.
/// </summary>
/// <param name="context">Database context shared with the other repositories of the request scope</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ScenarioVariantRepository : IScenarioVariantRepository
{
    private readonly DataBaseContext _context;

    public ScenarioVariantRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AnalyseScenario?> GetVariantAsync(Guid id, CancellationToken ct = default)
    {
        return await LiveVariants().FirstOrDefaultAsync(v => v.Id == id, ct);
    }

    public async Task<List<AnalyseScenario>> ListByGroupsAsync(IReadOnlyCollection<Guid> groupIds, CancellationToken ct = default)
    {
        var wanted = groupIds.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        return await LiveVariants()
            .AsNoTracking()
            .Where(v => v.GroupId != null && wanted.Contains(v.GroupId.Value))
            .OrderBy(v => v.Name)
            .ThenBy(v => v.Id)
            .ToListAsync(ct);
    }

    public async Task AddLinksAsync(Guid scenarioToken, IReadOnlyCollection<Guid> variantIds, CancellationToken ct = default)
    {
        var wanted = variantIds.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return;
        }

        var existing = await _context.Set<AnalyseScenarioVariant>()
            .Where(l => !l.IsDeleted && l.ScenarioToken == scenarioToken && wanted.Contains(l.VariantId))
            .Select(l => l.VariantId)
            .ToListAsync(ct);

        var links = wanted
            .Except(existing)
            .Select(variantId => new AnalyseScenarioVariant
            {
                Id = Guid.NewGuid(),
                ScenarioToken = scenarioToken,
                VariantId = variantId
            })
            .ToList();

        await _context.Set<AnalyseScenarioVariant>().AddRangeAsync(links, ct);
    }

    public async Task<List<Guid>> GetVariantIdsAsync(Guid scenarioToken, CancellationToken ct = default)
    {
        return await LiveVariantsOfScenario(scenarioToken)
            .OrderBy(v => v.Name)
            .ThenBy(v => v.Id)
            .Select(v => v.Id)
            .ToListAsync(ct);
    }

    public async Task<Dictionary<Guid, List<Guid>>> GetVariantIdsByScenarioTokensAsync(
        IReadOnlyCollection<Guid> scenarioTokens, CancellationToken ct = default)
    {
        var wanted = scenarioTokens.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        var rows = await (
            from link in _context.Set<AnalyseScenarioVariant>()
            join variant in LiveVariants() on link.VariantId equals variant.Id
            where !link.IsDeleted && wanted.Contains(link.ScenarioToken)
            orderby variant.Name, variant.Id
            select new { link.ScenarioToken, VariantId = variant.Id })
            .ToListAsync(ct);

        return rows
            .GroupBy(row => row.ScenarioToken)
            .ToDictionary(group => group.Key, group => group.Select(row => row.VariantId).ToList());
    }

    public async Task<List<AnalyseScenario>> GetVariantsOfScenarioAsync(Guid scenarioToken, CancellationToken ct = default)
    {
        return await LiveVariantsOfScenario(scenarioToken)
            .AsNoTracking()
            .OrderBy(v => v.Name)
            .ThenBy(v => v.Id)
            .ToListAsync(ct);
    }

    public async Task<List<AnalyseScenario>> GetVariantsByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        var wanted = ids.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        return await LiveVariants()
            .AsNoTracking()
            .Where(v => wanted.Contains(v.Id))
            .OrderBy(v => v.Name)
            .ThenBy(v => v.Id)
            .ToListAsync(ct);
    }

    public async Task<bool> HasActiveScenarioAsync(Guid variantId, CancellationToken ct = default)
    {
        return await (
            from link in _context.Set<AnalyseScenarioVariant>()
            join variant in _context.Set<AnalyseScenario>() on link.VariantId equals variant.Id
            join scenario in _context.Set<AnalyseScenario>() on link.ScenarioToken equals scenario.Token
            where !link.IsDeleted
                && link.VariantId == variantId
                && !variant.IsDeleted
                && variant.Kind == AnalyseScenarioKind.Variant
                && !scenario.IsDeleted
                && scenario.Kind == AnalyseScenarioKind.Scenario
                && scenario.Status == AnalyseScenarioStatus.Active
            select link.Id)
            .AnyAsync(ct);
    }

    public async Task<Dictionary<Guid, int>> CountShiftsByVariantTokenAsync(
        IReadOnlyCollection<Guid> tokens, CancellationToken ct = default)
    {
        var wanted = tokens.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        var counts = await _context.Shift
            .Where(s => !s.IsDeleted
                && s.Status == ShiftStatus.SealedOrder
                && s.AnalyseToken != null
                && wanted.Contains(s.AnalyseToken.Value))
            .GroupBy(s => s.AnalyseToken!.Value)
            .Select(g => new { Token = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var result = wanted.ToDictionary(token => token, _ => 0);
        foreach (var entry in counts)
        {
            result[entry.Token] = entry.Count;
        }

        return result;
    }

    public async Task<List<Guid>> GetShiftIdsByVariantTokensAsync(
        IReadOnlyCollection<Guid> variantTokens, CancellationToken ct = default)
    {
        var wanted = variantTokens.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return [];
        }

        return await _context.Shift
            .Where(s => !s.IsDeleted && s.AnalyseToken != null && wanted.Contains(s.AnalyseToken.Value))
            .Select(s => s.Id)
            .ToListAsync(ct);
    }

    public async Task<List<Shift>> GetSealedShiftsAsync(Guid variantToken, CancellationToken ct = default)
    {
        return await _context.Shift
            .AsNoTracking()
            .Include(s => s.Client)
                .ThenInclude(c => c!.Addresses)
            .Include(s => s.GroupItems)
                .ThenInclude(gi => gi.Group)
            .Include(s => s.ShiftExpenses)
            .Include(s => s.RequiredQualifications)
            .Where(s => !s.IsDeleted
                && s.Status == ShiftStatus.SealedOrder
                && s.AnalyseToken == variantToken)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);
    }

    public async Task<List<Guid>> GetShiftFamilyIdsAsync(Guid variantToken, Guid familyRootId, CancellationToken ct = default)
    {
        return await _context.Shift
            .Where(s => !s.IsDeleted
                && s.AnalyseToken == variantToken
                && (s.Id == familyRootId || s.OriginalId == familyRootId))
            .Select(s => s.Id)
            .ToListAsync(ct);
    }

    public async Task<bool> HasCutsAsync(Guid variantToken, Guid sealedOrderId, CancellationToken ct = default)
    {
        return await _context.Shift
            .AnyAsync(s => !s.IsDeleted
                && s.AnalyseToken == variantToken
                && s.Status == ShiftStatus.SplitShift
                && s.OriginalId == sealedOrderId, ct);
    }

    public async Task<bool> HasShiftsOutsidePeriodAsync(Guid variantToken, DateOnly periodFrom, DateOnly periodUntil, CancellationToken ct = default)
    {
        return await _context.Shift
            .AnyAsync(s => !s.IsDeleted
                && s.AnalyseToken == variantToken
                && (s.FromDate < periodFrom || s.UntilDate == null || s.UntilDate > periodUntil), ct);
    }

    public async Task<bool> HasScenarioClonesAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken ct = default)
    {
        var ids = shiftIds.ToList();

        return await _context.Shift.IgnoreQueryFilters()
            .AnyAsync(s => !s.IsDeleted && s.ScenarioSourceShiftId.HasValue && ids.Contains(s.ScenarioSourceShiftId.Value), ct);
    }

    public async Task SoftDeleteFinishedScenarioLinksAsync(Guid variantId, CancellationToken ct = default)
    {
        var links = await _context.Set<AnalyseScenarioVariant>()
            .Where(l => !l.IsDeleted
                && l.VariantId == variantId
                && !_context.Set<AnalyseScenario>().Any(s => !s.IsDeleted
                    && s.Kind == AnalyseScenarioKind.Scenario
                    && s.Token == l.ScenarioToken
                    && s.Status == AnalyseScenarioStatus.Active))
            .ToListAsync(ct);

        _context.Set<AnalyseScenarioVariant>().RemoveRange(links);
    }

    private IQueryable<AnalyseScenario> LiveVariants() => ScenarioVariantQueries.LiveVariants(_context);

    private IQueryable<AnalyseScenario> LiveVariantsOfScenario(Guid scenarioToken)
        => ScenarioVariantQueries.LiveVariantsOfScenario(_context, scenarioToken);
}
