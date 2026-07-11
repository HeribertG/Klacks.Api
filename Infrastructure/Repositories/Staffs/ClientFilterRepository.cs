// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientFilterRepository : IClientFilterRepository
{
    private const int FuzzyFallbackLimit = 50;

    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientFilterService _clientFilterService;
    private readonly IClientMembershipFilterService _membershipFilterService;
    private readonly IClientSearchService _searchService;
    private readonly IClientSortingService _sortingService;
    private readonly IClientFuzzySearchService _fuzzySearchService;

    public ClientFilterRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientFilterService clientFilterService,
        IClientMembershipFilterService membershipFilterService,
        IClientSearchService searchService,
        IClientSortingService sortingService,
        IClientFuzzySearchService fuzzySearchService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _clientFilterService = clientFilterService;
        _membershipFilterService = membershipFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _fuzzySearchService = fuzzySearchService;
    }

    public async Task<PagedResult<Client>> GetFilteredClients(ClientFilter filter, PaginationParams pagination)
    {
        var query = await FilterClients(filter);

        var totalCount = await query.CountAsync();

        // A spoken or misheard name ("Meier" vs "Mayer") never survives the substring search —
        // when it finds nothing, rank the query fuzzily (trigram + phonetics) and keep only
        // candidates the caller's regular filters (group scope, type, membership) permit.
        if (totalCount == 0 && pagination.PageIndex == 0 && IsFuzzyEligible(filter))
        {
            var fuzzyResult = await TryFuzzyFallbackAsync(filter, pagination);
            if (fuzzyResult != null)
            {
                return fuzzyResult;
            }
        }

        var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);
        if (pagination.PageIndex >= totalPages && totalCount > 0)
        {
            return new PagedResult<Client>
            {
                Items = new List<Client>(),
                TotalCount = totalCount,
                PageNumber = pagination.PageIndex,
                PageSize = pagination.PageSize
            };
        }

        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync();

        return new PagedResult<Client>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageIndex,
            PageSize = pagination.PageSize
        };
    }

    private bool IsFuzzyEligible(ClientFilter filter)
    {
        return !string.IsNullOrWhiteSpace(filter.SearchString)
               && !_searchService.IsNumericSearch(filter.SearchString)
               && !_searchService.IsMultipleNumericSearch(filter.SearchString);
    }

    private async Task<PagedResult<Client>?> TryFuzzyFallbackAsync(ClientFilter filter, PaginationParams pagination)
    {
        var ranked = await _fuzzySearchService.SearchAsync(filter.SearchString, FuzzyFallbackLimit) ?? [];
        if (ranked.Count == 0)
        {
            return null;
        }

        var rankedIds = ranked.Select(c => c.Id).ToList();
        var rankById = rankedIds
            .Select((id, index) => (Id: id, Index: index))
            .ToDictionary(x => x.Id, x => x.Index);

        // Re-run the regular pipeline without the search string so every other filter
        // (group visibility, entity type, membership) still applies to the fuzzy candidates,
        // and so the items carry the same includes as a normal page.
        var originalSearch = filter.SearchString;
        filter.SearchString = string.Empty;
        try
        {
            var permittedQuery = await FilterClients(filter);
            var permitted = await permittedQuery
                .Where(c => rankedIds.Contains(c.Id))
                .ToListAsync();
            if (permitted.Count == 0)
            {
                return null;
            }

            var items = permitted
                .OrderBy(c => rankById[c.Id])
                .Take(pagination.Take)
                .ToList();

            return new PagedResult<Client>
            {
                Items = items,
                TotalCount = permitted.Count,
                PageNumber = pagination.PageIndex,
                PageSize = pagination.PageSize
            };
        }
        finally
        {
            filter.SearchString = originalSearch;
        }
    }

    public async Task<IQueryable<Client>> FilterClients(ClientFilter filter)
    {
        IQueryable<Client> query;

        if (filter.ShowDeleteEntries)
        {
            query = this.context.Client.IgnoreQueryFilters()
                                .Include(cu => cu.Membership)
                                .Include(cu => cu.Addresses)
                                .Include(cu => cu.Communications)
                                .Include(cu => cu.Annotations)
                                .Include(cu => cu.BreakPlaceholders)
                                .Include(cu => cu.ClientContracts)
                                    .ThenInclude(cc => cc.Contract)
                                .AsSplitQuery()
                                .Where(cu => cu.IsDeleted)
                                .AsNoTracking()
                                .AsQueryable();
        }
        else
        {
            query = this.context.Client
                                .Include(cu => cu.Membership)
                                .Include(cu => cu.Addresses)
                                .Include(cu => cu.Communications)
                                .Include(cu => cu.Annotations)
                                .Include(cu => cu.BreakPlaceholders)
                                .Include(cu => cu.GroupItems)
                                .Include(cu => cu.ClientContracts)
                                    .ThenInclude(cc => cc.Contract)
                                .AsSplitQuery()
                                .AsNoTracking()
                                .AsQueryable();
        }

       query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query, filter.WithoutGroup);

        if (_searchService.IsMultipleNumericSearch(filter.SearchString))
        {
            var idNumbers = filter.SearchString
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.Parse(s.Trim()))
                .ToArray();
            query = _searchService.ApplyMultipleIdNumberSearch(query, idNumbers);
            query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        }
        else if (_searchService.IsNumericSearch(filter.SearchString))
        {
            var query1 = _searchService.ApplyIdNumberSearch(query, int.Parse(filter.SearchString.Trim()));

            if (filter.IncludeAddress)
            {
                var query2 = _searchService.ApplyPhoneOrZipSearch(query, filter.SearchString.Trim());
                query = query1.Union(query2);
            }
            else
            {
                query = query1;
            }

            query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        }
        else
        {
            query = _clientFilterService.ApplyEntityTypeFilter(query, filter.Employee, filter.ExternEmp, filter.Customer);

            var addressTypeList = _clientFilterService.CreateAddressTypeList(filter.HomeAddress, filter.CompanyAddress, filter.InvoiceAddress);
            var gender = _clientFilterService.CreateGenderList(filter.Male, filter.Female, filter.LegalEntity, filter.Intersexuality);
            var clientType = _clientFilterService.CreateClientTypeList(filter.Employee, filter.Customer, filter.ExternEmp);

            query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeAddress);

            if (!(filter.SearchOnlyByName.HasValue && filter.SearchOnlyByName.Value))
            {
                query = _clientFilterService.ApplyGenderFilter(query, gender);

                query = _clientFilterService.ApplyClientTypeFilter(query, clientType);

                query = _clientFilterService.ApplyAnnotationFilter(query, filter.HasAnnotation);

                query = _clientFilterService.ApplyAddressTypeFilter(query, addressTypeList);

                query = _clientFilterService.ApplyStateOrCountryFilter(query, filter.FilteredStateToken, filter.Countries);

                query = _membershipFilterService.ApplyMembershipFilter(query,
                                              filter.ActiveMembership.HasValue && filter.ActiveMembership.Value,
                                              filter.FormerMembership.HasValue && filter.FormerMembership.Value,
                                              filter.FutureMembership.HasValue && filter.FutureMembership.Value);

                if ((filter.ScopeFromFlag.HasValue || filter.ScopeUntilFlag.HasValue) &&
                    (filter.ScopeFrom.HasValue || filter.ScopeUntil.HasValue))
                {
                    query = _membershipFilterService.ApplyScopeFilter(query, filter.ScopeFromFlag, filter.ScopeUntilFlag, filter.ScopeFrom, filter.ScopeUntil);
                }
            }

            query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        }

        return query;
    }
}
