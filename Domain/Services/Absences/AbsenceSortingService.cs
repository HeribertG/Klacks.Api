// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sorting service for absence queries.
/// Uses MultiLanguageDbFunctions.ExtractText for language-dependent JSONB sorting,
/// dynamically supports all languages (core + plugin).
/// </summary>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Absences;

public class AbsenceSortingService : IAbsenceSortingService
{
    public IQueryable<Absence> ApplySorting(IQueryable<Absence> query, string orderBy, string sortOrder, string language)
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
            "description" => isAscending
                ? query.OrderBy(x => MultiLanguageDbFunctions.ExtractText(x.Description, lang))
                : query.OrderByDescending(x => MultiLanguageDbFunctions.ExtractText(x.Description, lang)),
            _ => query
        };
    }
}
