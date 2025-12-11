using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using System.Linq.Expressions;

namespace Klacks.Api.Domain.Services.ShiftSchedule;

public class ShiftScheduleSearchService : IShiftScheduleSearchService
{
    public IQueryable<ShiftDayAssignment> ApplySearchFilter(IQueryable<ShiftDayAssignment> query, string? searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return query;
        }

        var keywordList = ParseSearchString(searchString);

        if (keywordList.Length == 1 && keywordList[0].Length == 1)
        {
            return ApplyFirstSymbolSearch(query, keywordList[0]);
        }

        return ApplyKeywordSearch(query, keywordList);
    }

    private IQueryable<ShiftDayAssignment> ApplyKeywordSearch(IQueryable<ShiftDayAssignment> query, string[] keywords)
    {
        var normalizedKeywords = NormalizeKeywords(keywords);

        if (normalizedKeywords.Length == 0)
        {
            return query;
        }

        var predicate = PredicateBuilder.False<ShiftDayAssignment>();

        foreach (var keyword in normalizedKeywords)
        {
            predicate = predicate.Or(CreateSearchPredicate(keyword));
        }

        return query.Where(predicate);
    }

    private IQueryable<ShiftDayAssignment> ApplyFirstSymbolSearch(IQueryable<ShiftDayAssignment> query, string symbol)
    {
        var normalizedSymbol = symbol.ToLower();
        return query.Where(s => (s.ShiftName ?? "").ToLower().StartsWith(normalizedSymbol));
    }

    private static string[] ParseSearchString(string searchString)
    {
        return searchString.Trim().ToLower().Split(' ');
    }

    private static string[] NormalizeKeywords(string[] keywords)
    {
        return keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLower())
            .Distinct()
            .ToArray();
    }

    private static Expression<Func<ShiftDayAssignment, bool>> CreateSearchPredicate(string keyword)
    {
        return s =>
            (s.ShiftName ?? "").ToLower().Contains(keyword) ||
            (s.Abbreviation ?? "").ToLower().Contains(keyword);
    }
}
