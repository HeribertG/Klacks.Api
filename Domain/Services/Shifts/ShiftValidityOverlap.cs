// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The validity period filter of the shift list: a shift matches when its validity (FromDate..UntilDate, UntilDate empty
/// = open end) overlaps the chosen period, bounds inclusive. An empty lower bound reaches back without limit, an empty
/// upper bound forward without limit, and with both empty every shift matches. Written as one lambda over plain
/// columns so it translates to SQL.
/// </summary>
/// <param name="validFrom">First day of the chosen period; null = no lower bound</param>
/// <param name="validUntil">Last day of the chosen period; null = no upper bound</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftValidityOverlap
{
    public static bool HasPeriod(DateOnly? validFrom, DateOnly? validUntil) => validFrom.HasValue || validUntil.HasValue;

    public static Expression<Func<Shift, bool>> Overlaps(DateOnly? validFrom, DateOnly? validUntil)
    {
        var hasFrom = validFrom.HasValue;
        var from = validFrom.GetValueOrDefault();
        var hasUntil = validUntil.HasValue;
        var until = validUntil.GetValueOrDefault();

        return s => (!hasUntil || s.FromDate <= until)
            && (!hasFrom || s.UntilDate == null || s.UntilDate.Value >= from);
    }
}
