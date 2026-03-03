// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftSortingService : IShiftSortingService
{
    public IQueryable<Shift> ApplySorting(IQueryable<Shift> query, string orderBy, string sortOrder)
    {
        if (string.IsNullOrEmpty(sortOrder))
        {
            return query;
        }

        var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return orderBy?.ToLower() switch
        {
            "abbreviation" => isAscending
                ? query.OrderBy(x => x.Abbreviation)
                : query.OrderByDescending(x => x.Abbreviation),

            "name" => isAscending
                ? query.OrderBy(x => x.Name)
                : query.OrderByDescending(x => x.Name),

            "description" => isAscending
                ? query.OrderBy(x => x.Description)
                : query.OrderByDescending(x => x.Description),

            "valid_from" => isAscending
                ? query.OrderBy(x => x.FromDate)
                : query.OrderByDescending(x => x.FromDate),

            "valid_until" => isAscending
                ? query.OrderBy(x => x.UntilDate)
                : query.OrderByDescending(x => x.UntilDate),

            _ => query
        };
    }
}