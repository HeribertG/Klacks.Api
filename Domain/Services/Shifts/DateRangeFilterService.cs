// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filters a Shift query by active/former/future date range, relative to a caller-supplied "today"
/// (the company's own local day, resolved via ICompanyClock by the first async caller - this service
/// itself stays synchronous, matching Shift.FromDate/UntilDate, which are plain 'date' columns).
/// </summary>

using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using System.Linq.Expressions;

namespace Klacks.Api.Domain.Services.Shifts;

public class DateRangeFilterService : IDateRangeFilterService
{
    public IQueryable<Shift> ApplyDateRangeFilter(IQueryable<Shift> query, bool activeDateRange, bool formerDateRange, bool futureDateRange, DateOnly today)
    {
        if (activeDateRange && formerDateRange && futureDateRange)
        {
            return query; // No filters needed
        }

        if (!activeDateRange && !formerDateRange && !futureDateRange)
        {
            return Enumerable.Empty<Shift>().AsQueryable();
        }

        var predicate = PredicateBuilder.False<Shift>();

        if (activeDateRange)
        {
            predicate = predicate.Or(shift =>
                shift.FromDate <= today &&
                (!shift.UntilDate.HasValue || shift.UntilDate.Value >= today));
        }

        if (formerDateRange)
        {
            predicate = predicate.Or(shift =>
                shift.UntilDate.HasValue && shift.UntilDate.Value < today);
        }

        if (futureDateRange)
        {
            predicate = predicate.Or(shift => shift.FromDate > today);
        }

        return query.Where(predicate);
    }
}
