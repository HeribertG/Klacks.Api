using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Clients;

public class ClientSearchFilterService : IClientSearchFilterService
{
    private readonly IClientSearchService _searchService;

    public ClientSearchFilterService(IClientSearchService searchService)
    {
        _searchService = searchService;
    }

    public IQueryable<Client> ApplySearchFilter(IQueryable<Client> query, string searchString, bool includeAddress = false)
    {
        if (string.IsNullOrEmpty(searchString))
            return query;

        if (_searchService.IsNumericSearch(searchString))
        {
            var numericValue = int.Parse(searchString.Trim());
            return _searchService.ApplyIdNumberSearch(query, numericValue);
        }
        else
        {
            return _searchService.ApplySearchFilter(query, searchString, includeAddress);
        }
    }
}