// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure rules of a reset of a cut family. Without any real work the family goes back to the start of its sealed order,
/// whatever day was asked for; with work the asked day counts and has to lie after the last work. Only a row with real work
/// stays, together with its ancestors through ParentId, because a time, weekday or staff cut can leave a parent without work
/// above a child with work, and removing that parent would orphan the child. Every other row goes, open or closed.
/// ShiftFamilyEndPlanner does not fit: it trims the head and removes by date, a reset leaves the head alone and removes by work.
/// </summary>
/// <param name="orderFrom">First day of the sealed order</param>
/// <param name="lastWork">Last real work of the family, or null when there is none</param>
/// <param name="requested">The day that was asked for</param>
/// <param name="rows">The live rows below the sealed order, head excluded</param>
/// <param name="idsWithWork">Ids of the rows that carry real work</param>
/// <param name="start">The day the new plannable shift starts on</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftResetRules
{
    public static DateOnly EarliestAllowed(DateOnly orderFrom, DateOnly? lastWork) =>
        lastWork is { } day && day >= orderFrom ? day.AddDays(1) : orderFrom;

    public static DateOnly EffectiveStart(DateOnly orderFrom, DateOnly? lastWork, DateOnly requested) =>
        lastWork.HasValue && requested > orderFrom ? requested : orderFrom;

    public static bool HasNothingToReset(IReadOnlyCollection<Shift> rows, DateOnly start) =>
        rows.Any(row => row.Status == ShiftStatus.OriginalShift && IsOpenAt(row, start))
        && !rows.Any(row => row.Status == ShiftStatus.SplitShift && IsOpenAt(row, start));

    public static ShiftResetPlan Plan(IReadOnlyList<Shift> rows, IReadOnlySet<Guid> idsWithWork, DateOnly start)
    {
        var rowsById = rows.ToDictionary(row => row.Id);
        var kept = new HashSet<Guid>();
        foreach (var id in idsWithWork.Where(rowsById.ContainsKey))
        {
            KeepWithAncestors(id, rowsById, kept);
        }

        var keptRows = rows.Where(row => kept.Contains(row.Id)).ToList();

        return new ShiftResetPlan(
            keptRows.Where(row => IsOpenAt(row, start)).ToList(),
            rows.Where(row => !kept.Contains(row.Id)).ToList(),
            keptRows.Any(row => row.FromDate >= start));
    }

    private static void KeepWithAncestors(Guid id, Dictionary<Guid, Shift> rowsById, HashSet<Guid> kept)
    {
        Guid? current = id;
        while (current is { } rowId && rowsById.TryGetValue(rowId, out var row) && kept.Add(rowId))
        {
            current = row.ParentId;
        }
    }

    private static bool IsOpenAt(Shift row, DateOnly day) => row.UntilDate == null || row.UntilDate >= day;
}
