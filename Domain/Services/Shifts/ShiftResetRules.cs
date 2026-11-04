// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure rules of a reset of a cut family from a start day. The asked day counts, with or without real work; a day before the
/// sealed origin row is moved to its first day, and with real work the earliest allowed day is the one after the last work.
/// The rows are split by date, not by work: a row that begins before the start stays (also without work) and ends at the
/// latest the day before, so it can still be planned up to then; a row that begins on or after the start goes, open or
/// closed. The ancestors of a kept row stay too, because a time, weekday or staff cut can put a child below a parent, and
/// removing that parent would orphan the child. A kept ancestor that begins on or after the start cannot end before it;
/// the plan flags it, and the reset is refused.
/// </summary>
/// <param name="originFrom">First day of the sealed origin row</param>
/// <param name="lastWork">Last real work of the family, or null when there is none</param>
/// <param name="requested">The day that was asked for</param>
/// <param name="rows">The live rows below the sealed origin row, the origin row itself excluded</param>
/// <param name="start">The day the new plannable shift starts on</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftResetRules
{
    public static DateOnly EarliestAllowed(DateOnly originFrom, DateOnly? lastWork) =>
        lastWork is { } day && day >= originFrom ? day.AddDays(1) : originFrom;

    public static DateOnly EffectiveStart(DateOnly originFrom, DateOnly requested) =>
        requested > originFrom ? requested : originFrom;

    public static bool HasNothingToReset(IReadOnlyCollection<Shift> rows, DateOnly start) =>
        rows.Any(row => row.Status == ShiftStatus.OriginalShift && IsOpenAt(row, start))
        && !rows.Any(row => row.Status == ShiftStatus.SplitShift && IsOpenAt(row, start));

    public static ShiftResetPlan Plan(IReadOnlyList<Shift> rows, DateOnly start)
    {
        var rowsById = rows.ToDictionary(row => row.Id);
        var kept = new HashSet<Guid>();
        foreach (var row in rows.Where(row => row.FromDate < start))
        {
            KeepWithAncestors(row.Id, rowsById, kept);
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
