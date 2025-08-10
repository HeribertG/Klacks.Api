using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Models.Schedules;
using System.Linq.Expressions;

namespace Klacks.Api.Domain.Services.Shifts;

public class DateRangeFilterService : IDateRangeFilterService
{
    public IQueryable<Shift> ApplyDateRangeFilter(IQueryable<Shift> query, bool activeDateRange, bool formerDateRange, bool futureDateRange)
    {
        if (activeDateRange && formerDateRange && futureDateRange)
        {
            return query; // No filters needed
        }
        
        if (!activeDateRange && !formerDateRange && !futureDateRange)
        {
            return Enumerable.Empty<Shift>().AsQueryable();
        }

        var nowDateOnly = DateOnly.FromDateTime(DateTime.Now);
        var predicate = PredicateBuilder.False<Shift>();

        if (activeDateRange)
        {
            predicate = predicate.Or(shift => 
                shift.FromDate <= nowDateOnly && 
                (!shift.UntilDate.HasValue || shift.UntilDate.Value >= nowDateOnly));
        }

        if (formerDateRange)
        {
            predicate = predicate.Or(shift => 
                shift.UntilDate.HasValue && shift.UntilDate.Value < nowDateOnly);
        }

        if (futureDateRange)
        {
            predicate = predicate.Or(shift => shift.FromDate > nowDateOnly);
        }

        return query.Where(predicate);
    }

    public bool IsActiveShift(DateOnly fromDate, DateOnly? untilDate)
    {
        var nowDateOnly = DateOnly.FromDateTime(DateTime.Now);
        return fromDate <= nowDateOnly && 
               (!untilDate.HasValue || untilDate.Value >= nowDateOnly);
    }

    public bool IsFormerShift(DateOnly fromDate, DateOnly? untilDate)
    {
        var nowDateOnly = DateOnly.FromDateTime(DateTime.Now);
        return untilDate.HasValue && untilDate.Value < nowDateOnly;
    }

    public bool IsFutureShift(DateOnly fromDate, DateOnly? untilDate)
    {
        var nowDateOnly = DateOnly.FromDateTime(DateTime.Now);
        return fromDate > nowDateOnly;
    }

}