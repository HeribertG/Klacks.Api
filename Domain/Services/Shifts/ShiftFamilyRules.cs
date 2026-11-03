// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Pure rules of a shift family. A family with an order hangs on its sealed order through OriginalId, which is its head.
/// A family without an order is a container: one row that heads itself. Its end is the end of its rows once none is open.
/// </summary>
/// <param name="status">Lifecycle status of the row</param>
/// <param name="originalId">Order reference of the row</param>
/// <param name="shift">Row of a family</param>
/// <param name="family">The live rows of a family without an order</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftFamilyRules
{
    public static bool IsWithoutOrder(ShiftStatus status, Guid? originalId) =>
        originalId == null && status is ShiftStatus.OriginalShift or ShiftStatus.SplitShift;

    public static Guid HeadIdOf(Shift shift) => shift.OriginalId ?? shift.Id;

    public static DateOnly? StoredEndWithoutOrder(IReadOnlyCollection<Shift> family) =>
        family.Count == 0 || family.Any(row => row.UntilDate == null) ? null : family.Max(row => row.UntilDate);
}
