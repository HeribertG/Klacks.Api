// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule for the shape of a container (ShiftType.IsContainer). A container is an internal template that holds shifts,
/// not an order: once sealed it is one row with status OriginalShift and no OriginalId, there is no sealed order row and no
/// plannable copy. While its form is filled in it may still be a draft (status OriginalOrder). It is never cut; it is split
/// only in the Schedule through its Work children.
/// </summary>
/// <param name="shift">The row to judge or to bring into the shape</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ContainerShiftRules
{
    public static bool IsContainer(Shift shift) => shift.ShiftType == ShiftType.IsContainer;

    /// <summary>The same rule as a query filter on a work: the work stands on a container.</summary>
    public static readonly Expression<Func<Work, bool>> IsContainerWork =
        work => work.Shift != null && work.Shift.ShiftType == ShiftType.IsContainer;

    public static bool IsBeingSealed(Shift shift) => IsContainer(shift) && shift.Status == ShiftStatus.SealedOrder;

    public static bool HasOneRowShape(Shift shift) =>
        IsContainer(shift) && shift.Status == ShiftStatus.OriginalShift && shift.OriginalId == null;

    public static void ApplyOneRowShape(Shift shift)
    {
        shift.Status = ShiftStatus.OriginalShift;
        shift.OriginalId = null;
    }
}
