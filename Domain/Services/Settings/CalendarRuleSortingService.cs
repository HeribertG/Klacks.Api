// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sorting service for CalendarRule queries.
/// Uses MultiLanguageDbFunctions.ExtractText for language-dependent JSONB sorting,
/// dynamically supports all languages (core + plugin).
/// </summary>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Services.Settings;

public class CalendarRuleSortingService : ICalendarRuleSortingService
{
    public IQueryable<CalendarRule> ApplySorting(IQueryable<CalendarRule> query, string orderBy, string sortOrder, string language)
    {
        if (string.IsNullOrEmpty(sortOrder))
        {
            return query;
        }

        var lang = language?.ToLower() ?? "en";
        var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return orderBy?.ToLower() switch
        {
            "name" => isAscending
                ? query.OrderBy(x => MultiLanguageDbFunctions.ExtractText(x.Name, lang))
                : query.OrderByDescending(x => MultiLanguageDbFunctions.ExtractText(x.Name, lang)),
            "state" => isAscending
                ? query.OrderBy(x => x.State)
                : query.OrderByDescending(x => x.State),
            "country" => isAscending
                ? query.OrderBy(x => x.Country)
                : query.OrderByDescending(x => x.Country),
            "description" => isAscending
                ? query.OrderBy(x => MultiLanguageDbFunctions.ExtractText(x.Description, lang))
                : query.OrderByDescending(x => MultiLanguageDbFunctions.ExtractText(x.Description, lang)),
            _ => query
        };
    }
}
