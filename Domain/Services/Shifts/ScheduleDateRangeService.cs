// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Services.Shifts;

public class ScheduleDateRangeService : IScheduleDateRangeService
{
    public (DateOnly startDate, DateOnly endDate) CalculateScheduleDateRange(ShiftScheduleFilter filter)
    {
        return (filter.StartDate, filter.EndDate);
    }

    public IQueryable<Shift> ApplyScheduleDateFilter(IQueryable<Shift> query, DateOnly startDate, DateOnly endDate)
    {
        return query.Where(shift =>
            (shift.FromDate >= startDate && shift.FromDate <= endDate) ||
            (shift.UntilDate >= startDate && shift.UntilDate <= endDate) ||
            (shift.FromDate <= startDate && shift.UntilDate >= endDate))
            .OrderBy(shift => shift.FromDate)
            .ThenBy(shift => shift.UntilDate);
    }
}