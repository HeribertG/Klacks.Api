// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleSearchService : IShiftScheduleSearchService
{
    public IQueryable<ShiftDayAssignment> ApplySearchFilter(IQueryable<ShiftDayAssignment> query, string? searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return query;
        }

        if (searchString.Contains('+'))
        {
            var keywordList = searchString.ToLower().Split('+');
            return ApplyExactSearch(query, keywordList);
        }

        var keywords = ParseSearchString(searchString);

        if (keywords.Length == 1)
        {
            if (keywords[0].Length == 1)
            {
                return ApplyFirstSymbolSearch(query, keywords[0]);
            }

            return ApplyExactSearch(query, keywords);
        }

        return ApplyStandardSearch(query, keywords);
    }

    private static IQueryable<ShiftDayAssignment> ApplyExactSearch(IQueryable<ShiftDayAssignment> query, string[] keywords)
    {
        var predicate = PredicateBuilder.False<ShiftDayAssignment>();

        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var cleanKeyword = keyword.Trim().ToLower();

            predicate = predicate.Or(s =>
                (s.ShiftName ?? "").ToLower().Contains(cleanKeyword) ||
                (s.Abbreviation ?? "").ToLower().Contains(cleanKeyword));
        }

        return query.Where(predicate);
    }

    private static IQueryable<ShiftDayAssignment> ApplyStandardSearch(IQueryable<ShiftDayAssignment> query, string[] keywords)
    {
        var predicate = PredicateBuilder.True<ShiftDayAssignment>();

        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var cleanKeyword = keyword.Trim().ToLower();

            var keywordPredicate = PredicateBuilder.False<ShiftDayAssignment>();

            keywordPredicate = keywordPredicate.Or(s =>
                (s.ShiftName ?? "").ToLower().Contains(cleanKeyword) ||
                (s.Abbreviation ?? "").ToLower().Contains(cleanKeyword));

            predicate = predicate.And(keywordPredicate);
        }

        return query.Where(predicate);
    }

    private static IQueryable<ShiftDayAssignment> ApplyFirstSymbolSearch(IQueryable<ShiftDayAssignment> query, string symbol)
    {
        var cleanSymbol = symbol.ToLower();
        return query.Where(s =>
            (s.ShiftName ?? "").ToLower().StartsWith(cleanSymbol) ||
            (s.Abbreviation ?? "").ToLower().StartsWith(cleanSymbol));
    }

    private static string[] ParseSearchString(string searchString)
    {
        return searchString.Trim().ToLower()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
