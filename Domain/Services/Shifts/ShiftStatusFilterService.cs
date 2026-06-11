// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the shift list view filter. The views are mutually exclusive:
/// Original shows task orders (OriginalOrder, or SealedOrder when the sealed toggle is set),
/// Shift shows plannable tasks (Status >= OriginalShift), Container shows every container
/// shift regardless of status, Absence is a placeholder that returns no rows.
/// @param query - Shift query the view filter is applied to
/// @param filterType - Selected list view (Original, Shift, Container, Absence)
/// @param isSealedOrder - Toggles the Original view between OriginalOrder and SealedOrder rows
/// @param isTimeRange/isSporadic - Sub-filters of the Shift view
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftStatusFilterService : IShiftStatusFilterService
{
    public IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, ShiftFilterType filterType, bool isSealedOrder = false, bool isTimeRange = true, bool isSporadic = true)
    {
        return filterType switch
        {
            ShiftFilterType.Original => isSealedOrder
                ? query.Where(shift => shift.ShiftType == ShiftType.IsTask && shift.Status == ShiftStatus.SealedOrder)
                : query.Where(shift => shift.ShiftType == ShiftType.IsTask && shift.Status == ShiftStatus.OriginalOrder),

            ShiftFilterType.Shift => ApplyShiftTypeFilter(query, isTimeRange, isSporadic),

            ShiftFilterType.Container => query.Where(shift => shift.ShiftType == ShiftType.IsContainer),

            ShiftFilterType.Absence => query.Where(shift => false),

            _ => query
        };
    }

    private IQueryable<Shift> ApplyShiftTypeFilter(IQueryable<Shift> query, bool isTimeRange, bool isSporadic)
    {
        var baseQuery = query.Where(shift => shift.Status >= ShiftStatus.OriginalShift && shift.ShiftType == ShiftType.IsTask);

        if (isTimeRange && isSporadic)
        {
            return baseQuery;
        }
        else if (isTimeRange)
        {
            return baseQuery.Where(shift => shift.IsTimeRange);
        }
        else if (isSporadic)
        {
            return baseQuery.Where(shift => shift.IsSporadic);
        }
        else
        {
            return baseQuery.Where(shift => !shift.IsTimeRange && !shift.IsSporadic);
        }
    }
}