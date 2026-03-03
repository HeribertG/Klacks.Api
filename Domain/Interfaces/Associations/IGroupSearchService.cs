// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Domain service for group search and filtering operations
/// </summary>
public interface IGroupSearchService
{
    /// <summary>
    /// Applies comprehensive filtering to groups based on filter criteria
    /// </summary>
    /// <param name="query">Base query from repository</param>
    /// <param name="filter">Filter criteria including search string, date ranges, sorting</param>
    /// <returns>Filtered and sorted queryable of groups</returns>
    IQueryable<Group> ApplyFilters(IQueryable<Group> query, GroupFilter filter);

    /// <summary>
    /// Filters groups by search string using various search strategies
    /// </summary>
    /// <param name="query">Base query to filter</param>
    /// <param name="searchString">SearchString term</param>
    /// <returns>Filtered query matching search criteria</returns>
    IQueryable<Group> ApplySearchFilter(IQueryable<Group> query, string searchString);

    /// <summary>
    /// Applies first symbol search for single character searches
    /// </summary>
    /// <param name="query">Base query to filter</param>
    /// <param name="keyword">Single character search term</param>
    /// <returns>Groups starting with the specified character</returns>
    IQueryable<Group> ApplyFirstSymbolSearch(IQueryable<Group> query, string keyword);

    /// <summary>
    /// Applies standard multi-keyword search with AND logic
    /// </summary>
    /// <param name="query">Base query to filter</param>
    /// <param name="keywordList">Array of search keywords</param>
    /// <returns>Groups matching all keywords</returns>
    IQueryable<Group> ApplyMultipleKeywordSearch(IQueryable<Group> query, string[] keywordList);

    /// <summary>
    /// Sorts groups based on specified criteria
    /// </summary>
    /// <param name="query">Query to sort</param>
    /// <param name="orderBy">Field to order by (name, description, valid_from, valid_until)</param>
    /// <param name="sortOrder">Sort direction (asc, desc)</param>
    /// <returns>Sorted query</returns>
    IQueryable<Group> ApplySorting(IQueryable<Group> query, string orderBy, string sortOrder);

    /// <summary>
    /// Creates paginated results from filtered groups
    /// </summary>
    /// <param name="query">Filtered query from repository</param>
    /// <param name="filter">Filter containing pagination parameters</param>
    /// <returns>Truncated result with pagination info</returns>
    Task<TruncatedGroup> ApplyPaginationAsync(IQueryable<Group> query, GroupFilter filter);

    /// <summary>
    /// Parses search string into individual keywords
    /// </summary>
    /// <param name="searchString">Raw search input</param>
    /// <returns>Array of cleaned keywords</returns>
    string[] ParseSearchString(string searchString);

    /// <summary>
    /// Searches groups by name with partial matching
    /// </summary>
    /// <param name="query">Base query</param>
    /// <param name="name">Name to search for</param>
    /// <param name="exactMatch">Whether to use exact matching</param>
    /// <returns>Groups matching name criteria</returns>
    IQueryable<Group> ApplyNameSearch(IQueryable<Group> query, string name, bool exactMatch = false);

    /// <summary>
    /// Searches groups by description with partial matching
    /// </summary>
    /// <param name="query">Base query</param>
    /// <param name="description">Description to search for</param>
    /// <returns>Groups matching description criteria</returns>
    IQueryable<Group> ApplyDescriptionSearch(IQueryable<Group> query, string description);
}