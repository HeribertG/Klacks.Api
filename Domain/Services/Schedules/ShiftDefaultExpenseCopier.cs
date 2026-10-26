// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// Turns default expense rows of one shift into rows of another shift. Used where a cut creates a piece: the piece owns
/// copies of the rows of the shift it is cut from, never the source rows themselves, so the copies get fresh ids and a
/// later change on one shift does not reach the other.
/// </summary>
public static class ShiftDefaultExpenseCopier
{
    /// <summary>New rows with amount, description and taxable flag of <paramref name="sources"/> for <paramref name="targetShiftId"/>.</summary>
    /// <param name="sources">Rows to copy</param>
    /// <param name="targetShiftId">Shift the copies belong to</param>
    public static List<ShiftExpenses> CopyAll(IEnumerable<ShiftExpenses> sources, Guid targetShiftId)
        => sources.Select(source => new ShiftExpenses
        {
            Id = Guid.NewGuid(),
            ShiftId = targetShiftId,
            Amount = source.Amount,
            Description = source.Description,
            Taxable = source.Taxable
        }).ToList();
}
