// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Groups the coverage and the open slots of a scenario's planned shifts by the variant (key) they were cloned from.
/// A clone belongs to a variant when its ScenarioSourceShiftId points at a live shift that still carries the token of a
/// live variant the scenario is linked to; after the variant was promoted the token is gone and the clone belongs to none.
/// CopiedShiftCount is read from the variant, not from the clones: the accept promotes every live row of a linked variant,
/// so a sealed order of the variant whose CopiedFromShiftId names a live real order that overlaps the variant's period is
/// double demand after the accept, whatever the scenario planned. A variant with such an order is listed even when it plans
/// nothing in the scenario period, otherwise the warning would disappear exactly there. The sealed order is the one row that
/// carries the reference; pieces cut later do not need it and count nothing.
/// </summary>
/// <param name="context">Database context used for the lookups (linked variants, source shifts, copied orders)</param>
/// <param name="scenarioToken">Token of the scenario whose links are read</param>
/// <param name="plannedShifts">Planned shifts of the scenario whose source shifts are mapped</param>
/// <param name="keysByShift">Variant per scenario shift id, as returned by LoadKeysByShiftAsync</param>
/// <param name="coverage">Coverage per scenario shift</param>
/// <param name="openSlots">Open slots per scenario shift and day</param>
/// <param name="copiedByVariant">Name and count of copied orders per variant id, as returned by LoadCopiedCountsAsync; a variant without any has no entry</param>

using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

internal static class ScenarioVariantCoverageCalculator
{
    public static async Task<Dictionary<Guid, (Guid VariantId, string Name)>> LoadKeysByShiftAsync(
        DataBaseContext context,
        Guid scenarioToken,
        IReadOnlyCollection<Shift> plannedShifts,
        CancellationToken ct)
    {
        var keysByShift = new Dictionary<Guid, (Guid VariantId, string Name)>();

        var variants = await ScenarioVariantQueries.LiveVariantsOfScenario(context, scenarioToken)
            .AsNoTracking()
            .Select(v => new { v.Id, v.Name, v.Token })
            .ToListAsync(ct);
        if (variants.Count == 0)
        {
            return keysByShift;
        }

        var sourceIds = plannedShifts
            .Where(s => s.ScenarioSourceShiftId.HasValue)
            .Select(s => s.ScenarioSourceShiftId!.Value)
            .Distinct()
            .ToList();
        if (sourceIds.Count == 0)
        {
            return keysByShift;
        }

        var variantTokens = variants.Select(v => v.Token).ToList();
        var sources = await context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => sourceIds.Contains(s.Id)
                && !s.IsDeleted
                && s.AnalyseToken != null
                && variantTokens.Contains(s.AnalyseToken.Value))
            .Select(s => new { s.Id, Token = s.AnalyseToken!.Value })
            .ToListAsync(ct);

        var variantByToken = variants.ToDictionary(v => v.Token);
        var variantBySource = sources.ToDictionary(s => s.Id, s => variantByToken[s.Token]);
        foreach (var shift in plannedShifts)
        {
            if (shift.ScenarioSourceShiftId is { } sourceId && variantBySource.TryGetValue(sourceId, out var variant))
            {
                keysByShift[shift.Id] = (variant.Id, variant.Name);
            }
        }

        return keysByShift;
    }

    public static async Task<Dictionary<Guid, (string Name, int Count)>> LoadCopiedCountsAsync(
        DataBaseContext context,
        Guid scenarioToken,
        CancellationToken ct)
    {
        var counts = await (
            from variant in ScenarioVariantQueries.LiveVariantsOfScenario(context, scenarioToken)
            join order in context.Shift.IgnoreQueryFilters() on (Guid?)variant.Token equals order.AnalyseToken
            join original in context.Shift.IgnoreQueryFilters() on order.CopiedFromShiftId equals (Guid?)original.Id
            where !order.IsDeleted
                && order.Status == ShiftStatus.SealedOrder
                && !original.IsDeleted
                && original.AnalyseToken == null
                && original.ScenarioSourceShiftId == null
                && original.FromDate <= variant.UntilDate
                && (original.UntilDate == null || original.UntilDate >= variant.FromDate)
            group order by new { variant.Id, variant.Name } into orders
            select new { orders.Key.Id, orders.Key.Name, Count = orders.Count() })
            .ToListAsync(ct);

        return counts.ToDictionary(entry => entry.Id, entry => (entry.Name, entry.Count));
    }

    public static IReadOnlyList<ScenarioVariantCoverageDto>? Summarize(
        IReadOnlyDictionary<Guid, (Guid VariantId, string Name)> keysByShift,
        IReadOnlyList<ScenarioShiftCoverageDto> coverage,
        IReadOnlyList<(Shift Shift, DateOnly Date, int Open)> openSlots,
        IReadOnlyDictionary<Guid, (string Name, int Count)> copiedByVariant)
    {
        if (keysByShift.Count == 0 && copiedByVariant.Count == 0)
        {
            return null;
        }

        var openByVariantAndDay = openSlots
            .Where(slot => keysByShift.ContainsKey(slot.Shift.Id))
            .GroupBy(slot => (keysByShift[slot.Shift.Id].VariantId, slot.Date))
            .ToDictionary(group => group.Key, group => group.Sum(slot => slot.Open));

        var planned = coverage
            .Where(c => keysByShift.ContainsKey(c.ShiftId))
            .GroupBy(c => keysByShift[c.ShiftId])
            .Select(group => ToDto(
                group.Key.VariantId, group.Key.Name, group, openByVariantAndDay, copiedByVariant.GetValueOrDefault(group.Key.VariantId).Count))
            .ToList();
        var plannedIds = planned.Select(dto => dto.VariantId).ToHashSet();
        var plannedNothing = copiedByVariant
            .Where(entry => !plannedIds.Contains(entry.Key))
            .Select(entry => ToDto(entry.Key, entry.Value.Name, [], openByVariantAndDay, entry.Value.Count));

        return planned
            .Concat(plannedNothing)
            .OrderBy(dto => dto.Name, StringComparer.CurrentCulture)
            .ThenBy(dto => dto.VariantId)
            .ToList();
    }

    private static ScenarioVariantCoverageDto ToDto(
        Guid variantId,
        string name,
        IEnumerable<ScenarioShiftCoverageDto> shifts,
        IReadOnlyDictionary<(Guid VariantId, DateOnly Date), int> openByVariantAndDay,
        int copiedShiftCount)
    {
        var demanded = 0;
        var filled = 0;
        foreach (var shift in shifts)
        {
            demanded += shift.DemandedSlots;
            filled += shift.FilledSlots;
        }

        var peakOpen = 0;
        DateOnly? peakDate = null;
        foreach (var ((id, date), open) in openByVariantAndDay.OrderBy(entry => entry.Key.Date))
        {
            if (id == variantId && open > peakOpen)
            {
                peakOpen = open;
                peakDate = date;
            }
        }

        return new ScenarioVariantCoverageDto(variantId, name, demanded, filled, peakOpen, peakDate, copiedShiftCount);
    }
}
