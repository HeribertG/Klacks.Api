// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IDateRangeFilterService
{
    IQueryable<Shift> ApplyDateRangeFilter(IQueryable<Shift> query, bool activeDateRange, bool formerDateRange, bool futureDateRange);
   
    bool IsActiveShift(DateOnly fromDate, DateOnly? untilDate);
    
    bool IsFormerShift(DateOnly fromDate, DateOnly? untilDate);
    
    bool IsFutureShift(DateOnly fromDate, DateOnly? untilDate);
}