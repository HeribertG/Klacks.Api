using Klacks.Api.Extensions;
using Klacks.Api.Helper;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Models.Schedules;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Klacks.Api.Services.Shifts;

public class ShiftSearchService : IShiftSearchService
{
    public IQueryable<Shift> ApplySearchFilter(IQueryable<Shift> query, string searchString, bool includeClient)
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

        return ApplyKeywordSearch(query, keywordList, includeClient);
    }

    public IQueryable<Shift> ApplyKeywordSearch(IQueryable<Shift> query, string[] keywords, bool includeClient)
    {
        var normalizedKeywords = NormalizeKeywords(keywords);

        if (normalizedKeywords.Length == 0)
        {
            return query;
        }

        var predicate = PredicateBuilder.False<Shift>();

        foreach (var keyword in normalizedKeywords)
        {
            predicate = predicate.Or(CreateShiftSearchPredicate(keyword));

            if (includeClient)
            {
                predicate = predicate.Or(CreateClientSearchPredicate(keyword));
            }
        }

        return query.Where(predicate);
    }

    public IQueryable<Shift> ApplyFirstSymbolSearch(IQueryable<Shift> query, string symbol)
    {
        var normalizedSymbol = symbol.ToLower();
        return query.Where(shift => (shift.Name ?? "").ToLower().StartsWith(normalizedSymbol));
    }

    private string[] ParseSearchString(string searchString)
    {
        return searchString.TrimEnd().TrimStart().ToLower().Split(' ');
    }

    private string[] NormalizeKeywords(string[] keywords)
    {
        return keywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k.Trim().ToLower())
            .Distinct()
            .ToArray();
    }

    private static Expression<Func<Shift, bool>> CreateShiftSearchPredicate(string keyword)
    {
        return shift =>
            (shift.Name ?? "").ToLower().Contains(keyword) ||
            (shift.Abbreviation ?? "").ToLower().Contains(keyword);
    }

    private static Expression<Func<Shift, bool>> CreateClientSearchPredicate(string keyword)
    {
        return shift => shift.Client != null && (
            (shift.Client.FirstName ?? "").ToLower().Contains(keyword) ||
            (shift.Client.SecondName ?? "").ToLower().Contains(keyword) ||
            (shift.Client.Name ?? "").ToLower().Contains(keyword) ||
            (shift.Client.MaidenName ?? "").ToLower().Contains(keyword) ||
            (shift.Client.Company ?? "").ToLower().Contains(keyword)
        );
    }
}