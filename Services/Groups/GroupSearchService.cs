using Klacks.Api.Datas;
using Klacks.Api.Interfaces.Domains;
using Klacks.Api.Models.Associations;
using Klacks.Api.Resources.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Services.Groups;

public class GroupSearchService : IGroupSearchService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupSearchService> _logger;
    private readonly IGroupValidityService _validityService;

    public GroupSearchService(DataBaseContext context, ILogger<GroupSearchService> logger, IGroupValidityService validityService)
    {
        _context = context;
        _logger = logger;
        _validityService = validityService;
    }

    public IQueryable<Group> ApplyFilters(IQueryable<Group> query, GroupFilter filter)
    {
        _logger.LogInformation("Applying search filters to query");

        // Apply date range filtering using validity service
        query = _validityService.ApplyDateRangeFilter(query, filter.ActiveDateRange, filter.FormerDateRange, filter.FutureDateRange);
        query = ApplySearchFilter(query, filter.SearchString);
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder);

        return query;
    }

    public IQueryable<Group> ApplySearchFilter(IQueryable<Group> query, string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            _logger.LogDebug("No search string provided, returning original query");
            return query;
        }

        _logger.LogInformation("Applying search filter for: {SearchString}", searchString);

        var keywords = ParseSearchString(searchString);

        if (keywords.Length == 1)
        {
            return ApplyFirstSymbolSearch(query, keywords[0]);
        }
        else
        {
            return ApplyMultipleKeywordSearch(query, keywords);
        }
    }

    public IQueryable<Group> ApplyFirstSymbolSearch(IQueryable<Group> query, string keyword)
    {
        var trimmedKeyword = keyword.Trim().ToLower();
        _logger.LogDebug("Applying first symbol search for: {Keyword}", trimmedKeyword);

        return query.Where(g => g.Name.ToLower().Contains(trimmedKeyword));
    }

    public IQueryable<Group> ApplyMultipleKeywordSearch(IQueryable<Group> query, string[] keywordList)
    {
        _logger.LogDebug("Applying multi-keyword search for {Count} keywords", keywordList.Length);

        foreach (var keyword in keywordList)
        {
            query = ApplyFirstSymbolSearch(query, keyword);
        }

        return query;
    }

    public IQueryable<Group> ApplySorting(IQueryable<Group> query, string orderBy, string sortOrder)
    {
        if (string.IsNullOrEmpty(sortOrder))
        {
            _logger.LogDebug("No sort order specified, returning original query");
            return query;
        }

        _logger.LogDebug("Applying sorting: {OrderBy} {SortOrder}", orderBy, sortOrder);

        return orderBy switch
        {
            "name" => sortOrder == "asc" ? query.OrderBy(x => x.Name) : query.OrderByDescending(x => x.Name),
            "description" => sortOrder == "asc" ? query.OrderBy(x => x.Description) : query.OrderByDescending(x => x.Description),
            "valid_from" => sortOrder == "asc" ? query.OrderBy(x => x.ValidFrom) : query.OrderByDescending(x => x.ValidFrom),
            "valid_until" => sortOrder == "asc" ? query.OrderBy(x => x.ValidUntil) : query.OrderByDescending(x => x.ValidUntil),
            _ => query
        };
    }

    public async Task<TruncatedGroup> ApplyPaginationAsync(IQueryable<Group> query, GroupFilter filter)
    {
        _logger.LogInformation("Applying pagination to query");
        var totalCount = query.Count();

        var maxPage = filter.NumberOfItemsPerPage > 0 ? (totalCount / filter.NumberOfItemsPerPage) : 0;
        var firstItem = CalculateFirstItem(filter, totalCount);

        var paginatedQuery = query.Skip(firstItem).Take(filter.NumberOfItemsPerPage);
        var groups = totalCount == 0 ? new List<Group>() : await paginatedQuery.ToListAsync();

        var result = new TruncatedGroup
        {
            Groups = groups,
            MaxItems = totalCount,
            CurrentPage = filter.RequiredPage,
            FirstItemOnPage = totalCount <= firstItem ? -1 : firstItem
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            result.MaxPages = totalCount % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
        }

        _logger.LogInformation("Created paginated result with {Count} groups, page {Page} of {MaxPages}", 
            groups.Count, result.CurrentPage, result.MaxPages);

        return result;
    }

    public string[] ParseSearchString(string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
        {
            return Array.Empty<string>();
        }

        return searchString.Trim().ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    public IQueryable<Group> ApplyNameSearch(IQueryable<Group> query, string name, bool exactMatch = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return query;
        }

        var searchTerm = name.Trim().ToLower();
        _logger.LogDebug("Searching by name: {Name}, exactMatch: {ExactMatch}", searchTerm, exactMatch);

        return exactMatch 
            ? query.Where(g => g.Name.ToLower() == searchTerm)
            : query.Where(g => g.Name.ToLower().Contains(searchTerm));
    }

    public IQueryable<Group> ApplyDescriptionSearch(IQueryable<Group> query, string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return query;
        }

        var searchTerm = description.Trim().ToLower();
        _logger.LogDebug("Searching by description: {Description}", searchTerm);

        return query.Where(g => g.Description != null && g.Description.ToLower().Contains(searchTerm));
    }


    private int CalculateFirstItem(GroupFilter filter, int totalCount)
    {
        if (totalCount == 0 || totalCount <= filter.NumberOfItemsPerPage)
        {
            return filter.RequiredPage * filter.NumberOfItemsPerPage;
        }

        if ((filter.IsNextPage.HasValue || filter.IsPreviousPage.HasValue) && filter.FirstItemOnLastPage.HasValue)
        {
            if (filter.IsNextPage.HasValue)
            {
                return filter.FirstItemOnLastPage.Value + filter.NumberOfItemsPerPage;
            }
            else
            {
                var numberOfItems = filter.NumberOfItemOnPreviousPage ?? filter.NumberOfItemsPerPage;
                var firstItem = filter.FirstItemOnLastPage.Value - numberOfItems;
                return Math.Max(0, firstItem);
            }
        }

        return filter.RequiredPage * filter.NumberOfItemsPerPage;
    }

}