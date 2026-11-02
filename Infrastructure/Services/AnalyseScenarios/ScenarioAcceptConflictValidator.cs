// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Conflict checks before an accept: refuses a scenario whose works sit on a scenario shift without a real root, whose
/// source shifts no longer exist or were deleted, whose source is a shift of a variant (key) the scenario does not carry,
/// whose work sits directly on a shift of a variant it does not carry, or whose source shift subtree changed after the
/// scenario was created (a concurrent cut by another user). The source of
/// a clone of a key shift is the key shift itself, and the children of the keys linked to the scenario count like real
/// children. Each refusal is a ConflictException (409).
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioAcceptConflictValidator
{
    public const string UncarriedVariantSourceMessage =
        "Cannot accept scenario: it holds shifts of a variant it does not carry.";

    public const string WorkOnUncarriedVariantShiftMessage =
        "Cannot accept scenario: it holds works on shifts of a variant it does not carry.";

    private const string RootlessScenarioShiftMessage =
        "Cannot accept scenario: works are planned on scenario shifts that have no shift in the original plan: {0}.";

    private readonly DataBaseContext _context;

    public ScenarioAcceptConflictValidator(DataBaseContext context)
    {
        _context = context;
    }

    public async Task ValidateNoAcceptConflictsAsync(Guid token, CancellationToken ct)
    {
        await EnsureNoRootlessScenarioShiftInUseAsync(token, ct);

        var variantTokens = await ScenarioVariantQueries.LiveVariantsOfScenario(_context, token)
            .Select(variant => variant.Token)
            .ToListAsync(ct);

        await EnsureNoWorkOnUncarriedVariantShiftAsync(token, variantTokens, ct);

        var clones = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId.HasValue)
            .Select(s => new { s.Id, SourceId = s.ScenarioSourceShiftId!.Value, s.SourceChildCountSnapshot })
            .ToListAsync(ct);

        if (clones.Count == 0) return;

        var sourceIds = clones.Select(c => c.SourceId).Distinct().ToList();

        var existingSources = await _context.Shift.IgnoreQueryFilters()
            .Where(s => sourceIds.Contains(s.Id))
            .Select(s => new { s.Id, s.IsDeleted, s.AnalyseToken })
            .ToListAsync(ct);

        var sourcesById = existingSources.ToDictionary(s => s.Id, s => s.IsDeleted);
        var sourceTokens = existingSources
            .Where(s => s.AnalyseToken.HasValue)
            .Select(s => s.AnalyseToken!.Value)
            .ToList();

        var missing = sourceIds.Where(id => !sourcesById.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            throw new ConflictException(
                $"Cannot accept scenario: source shifts no longer exist: {string.Join(", ", missing)}.");
        }

        var deleted = sourceIds.Where(id => sourcesById[id]).ToList();
        if (deleted.Count > 0)
        {
            throw new ConflictException(
                $"Cannot accept scenario: source shifts have been deleted: {string.Join(", ", deleted)}.");
        }

        await EnsureSourcesAreCarriedAsync(sourceTokens, variantTokens, ct);

        var currentChildCounts = await ScenarioSourceChildCounter.CountAsync(_context, sourceIds, variantTokens, ct);

        foreach (var clone in clones)
        {
            var snapshot = clone.SourceChildCountSnapshot ?? 0;
            var current = currentChildCounts.GetValueOrDefault(clone.SourceId, 0);
            if (snapshot != current)
            {
                throw new ConflictException(
                    $"Cannot accept scenario: source shift {clone.SourceId} subtree changed after scenario creation (children snapshot={snapshot}, current={current}). Likely a concurrent cut by another user.");
            }
        }
    }

    /// <summary>
    /// Refuses a scenario with a work sitting directly on a shift of a key it does not carry: the accept would make that
    /// work real on a shift that keeps its token, and a scenario without keys would pass the variant accept rule unnoticed.
    /// The second line of defence behind IWorkWriteGuard, for rows written before that check or past it.
    /// </summary>
    /// <param name="token">Token of the scenario being accepted</param>
    /// <param name="carriedTokens">Tokens of the keys the scenario carries</param>
    private async Task EnsureNoWorkOnUncarriedVariantShiftAsync(Guid token, IReadOnlyCollection<Guid> carriedTokens, CancellationToken ct)
    {
        var holdsWork = await (
            from work in _context.Work.IgnoreQueryFilters()
            join shift in _context.Shift.IgnoreQueryFilters() on work.ShiftId equals shift.Id
            join variant in _context.Set<AnalyseScenario>().IgnoreQueryFilters() on shift.AnalyseToken equals (Guid?)variant.Token
            where work.AnalyseToken == token
                && !work.IsDeleted
                && variant.Kind == AnalyseScenarioKind.Variant
                && !carriedTokens.Contains(variant.Token)
            select work.Id)
            .AnyAsync(ct);
        if (holdsWork)
        {
            throw new ConflictException(WorkOnUncarriedVariantShiftMessage);
        }
    }

    /// <summary>
    /// Refuses a clone whose source is a shift of a key the scenario does not carry: the accept promotes only the keys the
    /// scenario carries, so the works of that clone would land on a shift that keeps its token and hide from the real plan.
    /// </summary>
    /// <param name="sourceTokens">Tokens the source shifts of the clones carry</param>
    /// <param name="variantTokens">Tokens of the keys the scenario carries</param>
    private async Task EnsureSourcesAreCarriedAsync(
        IReadOnlyCollection<Guid> sourceTokens, IReadOnlyCollection<Guid> variantTokens, CancellationToken ct)
    {
        var uncarriedTokens = sourceTokens.Except(variantTokens).ToList();
        if (uncarriedTokens.Count == 0)
        {
            return;
        }

        var holdsUncarriedKey = await ScenarioVariantQueries.LiveVariants(_context)
            .AnyAsync(variant => uncarriedTokens.Contains(variant.Token), ct);
        if (holdsUncarriedKey)
        {
            throw new ConflictException(UncarriedVariantSourceMessage);
        }
    }

    /// <summary>
    /// Refuses to accept a scenario whose works sit on a scenario shift without a real root: promote would leave those
    /// works on a shift it soft-deletes. The clone of a key shift is not rootless: its root is the key shift, which turns
    /// real in the same commit. No current write path creates a rootless shift; the guard keeps it a clean 409 instead of
    /// orphaned real works should one appear.
    /// </summary>
    private async Task EnsureNoRootlessScenarioShiftInUseAsync(Guid token, CancellationToken ct)
    {
        var rootlessShiftIds = await _context.Shift.IgnoreQueryFilters()
            .Where(s => s.AnalyseToken == token && !s.IsDeleted && s.ScenarioSourceShiftId == null)
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (rootlessShiftIds.Count == 0)
        {
            return;
        }

        var usedShiftIds = await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && !w.IsDeleted && rootlessShiftIds.Contains(w.ShiftId))
            .Select(w => w.ShiftId)
            .Distinct()
            .ToListAsync(ct);
        if (usedShiftIds.Count > 0)
        {
            throw new ConflictException(string.Format(RootlessScenarioShiftMessage, string.Join(", ", usedShiftIds)));
        }
    }
}
