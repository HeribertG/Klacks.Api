using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public class ShiftSearchService : IShiftSearchService
{
    public IQueryable<Shift> ApplySearchFilter(IQueryable<Shift> query, string searchString, bool includeClient)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return query;
        }

        if (searchString.Contains('+'))
        {
            var keywordList = searchString.ToLower().Split('+');
            return ApplyExactSearch(query, keywordList, includeClient);
        }

        var keywords = ParseSearchString(searchString);

        if (keywords.Length == 1)
        {
            if (keywords[0].Length == 1)
            {
                return ApplyFirstSymbolSearch(query, keywords[0]);
            }

            return ApplyExactSearch(query, keywords, includeClient);
        }

        return ApplyStandardSearch(query, keywords, includeClient);
    }

    public IQueryable<Shift> ApplyExactSearch(IQueryable<Shift> query, string[] keywords, bool includeClient)
    {
        var predicate = PredicateBuilder.False<Shift>();

        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var cleanKeyword = keyword.Trim().ToLower();

            predicate = predicate.Or(s =>
                (s.Name ?? "").ToLower().Contains(cleanKeyword) ||
                (s.Abbreviation ?? "").ToLower().Contains(cleanKeyword));

            if (includeClient)
            {
                predicate = predicate.Or(s => s.Client != null && (
                    (s.Client.FirstName ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.SecondName ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.Name ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.MaidenName ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.Company ?? "").ToLower().Contains(cleanKeyword)));
            }
        }

        return query.Where(predicate);
    }

    public IQueryable<Shift> ApplyStandardSearch(IQueryable<Shift> query, string[] keywords, bool includeClient)
    {
        var predicate = PredicateBuilder.True<Shift>();

        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var cleanKeyword = keyword.Trim().ToLower();

            var keywordPredicate = PredicateBuilder.False<Shift>();

            keywordPredicate = keywordPredicate.Or(s =>
                (s.Name ?? "").ToLower().Contains(cleanKeyword) ||
                (s.Abbreviation ?? "").ToLower().Contains(cleanKeyword));

            if (includeClient)
            {
                keywordPredicate = keywordPredicate.Or(s => s.Client != null && (
                    (s.Client.FirstName ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.SecondName ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.Name ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.MaidenName ?? "").ToLower().Contains(cleanKeyword) ||
                    (s.Client.Company ?? "").ToLower().Contains(cleanKeyword)));
            }

            predicate = predicate.And(keywordPredicate);
        }

        return query.Where(predicate);
    }

    public IQueryable<Shift> ApplyFirstSymbolSearch(IQueryable<Shift> query, string symbol)
    {
        var cleanSymbol = symbol.ToLower();
        return query.Where(s =>
            (s.Name ?? "").ToLower().StartsWith(cleanSymbol) ||
            (s.Abbreviation ?? "").ToLower().StartsWith(cleanSymbol));
    }

    private static string[] ParseSearchString(string searchString)
    {
        return searchString.Trim().ToLower()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
