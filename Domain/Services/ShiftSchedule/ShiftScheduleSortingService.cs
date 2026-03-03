// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleSortingService : IShiftScheduleSortingService
{
    public IQueryable<ShiftDayAssignment> ApplySorting(IQueryable<ShiftDayAssignment> query, string? orderBy, string? sortOrder)
    {
        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return query;
        }

        var isAscending = string.IsNullOrWhiteSpace(sortOrder) || sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return orderBy.ToLower() switch
        {
            "name" => isAscending
                ? query.OrderBy(x => x.ShiftName)
                : query.OrderByDescending(x => x.ShiftName),

            "abbreviation" => isAscending
                ? query.OrderBy(x => x.Abbreviation)
                : query.OrderByDescending(x => x.Abbreviation),

            "startshift" => isAscending
                ? query.OrderBy(x => x.StartShift)
                : query.OrderByDescending(x => x.StartShift),

            "worktime" => isAscending
                ? query.OrderBy(x => x.WorkTime)
                : query.OrderByDescending(x => x.WorkTime),

            "date" => isAscending
                ? query.OrderBy(x => x.Date)
                : query.OrderByDescending(x => x.Date),

            _ => query
        };
    }
}
