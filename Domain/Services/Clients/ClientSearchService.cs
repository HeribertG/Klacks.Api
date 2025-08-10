using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientSearchService : IClientSearchService
{
    public IQueryable<Client> ApplySearchFilter(IQueryable<Client> query, string searchString, bool includeAddress)
    {
        if (string.IsNullOrEmpty(searchString))
        {
            return query;
        }

        if (searchString.Contains("+"))
        {
            var keywordList = searchString.ToLower().Split("+");
            return ApplyExactSearch(query, keywordList, includeAddress);
        }
        else
        {
            var keywordList = ParseSearchString(searchString);

            if (keywordList.Length == 1)
            {
                if (keywordList[0].Length == 1)
                {
                    return ApplyFirstSymbolSearch(query, keywordList[0]);
                }
                else
                {
                    return ApplyExactSearch(query, keywordList, includeAddress);
                }
            }
            else
            {
                return ApplyStandardSearch(query, keywordList, includeAddress);
            }
        }
    }

    public IQueryable<Client> ApplyExactSearch(IQueryable<Client> query, string[] keywords, bool includeAddress)
    {
        var predicate = PredicateBuilder.False<Client>();

        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var cleanKeyword = keyword.Trim().ToLower();
            
            predicate = predicate.Or(co =>
                (co.Name ?? "").ToLower().Contains(cleanKeyword) ||
                (co.FirstName ?? "").ToLower().Contains(cleanKeyword) ||
                (co.SecondName ?? "").ToLower().Contains(cleanKeyword) ||
                (co.MaidenName ?? "").ToLower().Contains(cleanKeyword) ||
                (co.Company ?? "").ToLower().Contains(cleanKeyword));

            if (includeAddress)
            {
                predicate = predicate.Or(co => co.Addresses.Any(ad =>
                    (ad.Street ?? "").ToLower().Contains(cleanKeyword) ||
                    (ad.City ?? "").ToLower().Contains(cleanKeyword) ||
                    (ad.State ?? "").ToLower().Contains(cleanKeyword) ||
                    (ad.Country ?? "").ToLower().Contains(cleanKeyword)));
            }
        }

        return query.Where(predicate);
    }

    public IQueryable<Client> ApplyStandardSearch(IQueryable<Client> query, string[] keywords, bool includeAddress)
    {
        var predicate = PredicateBuilder.True<Client>();

        foreach (var keyword in keywords.Where(k => !string.IsNullOrWhiteSpace(k)))
        {
            var cleanKeyword = keyword.Trim().ToLower();
            
            var keywordPredicate = PredicateBuilder.False<Client>();
            
            keywordPredicate = keywordPredicate.Or(co =>
                (co.Name ?? "").ToLower().Contains(cleanKeyword) ||
                (co.FirstName ?? "").ToLower().Contains(cleanKeyword) ||
                (co.SecondName ?? "").ToLower().Contains(cleanKeyword) ||
                (co.MaidenName ?? "").ToLower().Contains(cleanKeyword) ||
                (co.Company ?? "").ToLower().Contains(cleanKeyword));

            if (includeAddress)
            {
                keywordPredicate = keywordPredicate.Or(co => co.Addresses.Any(ad =>
                    (ad.Street ?? "").ToLower().Contains(cleanKeyword) ||
                    (ad.City ?? "").ToLower().Contains(cleanKeyword) ||
                    (ad.State ?? "").ToLower().Contains(cleanKeyword) ||
                    (ad.Country ?? "").ToLower().Contains(cleanKeyword)));
            }

            predicate = predicate.And(keywordPredicate);
        }

        return query.Where(predicate);
    }

    public IQueryable<Client> ApplyFirstSymbolSearch(IQueryable<Client> query, string keyword)
    {
        var cleanKeyword = keyword.ToLower();
        return query.Where(co => (co.Name ?? "").ToLower().StartsWith(cleanKeyword) ||
                                (co.FirstName ?? "").ToLower().StartsWith(cleanKeyword) ||
                                (co.Company ?? "").ToLower().StartsWith(cleanKeyword));
    }

    public IQueryable<Client> ApplyPhoneOrZipSearch(IQueryable<Client> query, string number)
    {
        return query.Where(co =>
                  co.Communications.Any(com => (com.Type == CommunicationTypeEnum.EmergencyPhone ||
                                                com.Type == CommunicationTypeEnum.PrivateCellPhone ||
                                                com.Type == CommunicationTypeEnum.OfficeCellPhone) && com.Value == number) ||
                  co.Addresses.Any(ad => ad.Zip.Trim() == number));
    }

    public IQueryable<Client> ApplyIdNumberSearch(IQueryable<Client> query, int idNumber)
    {
        return query.Where(x => x.IdNumber == idNumber);
    }

    public string[] ParseSearchString(string searchString)
    {
        return searchString.TrimEnd().TrimStart().ToLower()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    public bool IsNumericSearch(string searchString)
    {
        return searchString.Trim().All(char.IsNumber) && int.TryParse(searchString.Trim(), out _);
    }
}