// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Central service for client base queries.
/// Unifies membership, group, search, type filters and sorting for all client lists.
/// Returns IQueryable - Count, paging and includes are added by the caller.
/// </summary>
/// <param name="context">Database context</param>
/// <param name="groupFilterService">Service for group filtering</param>
/// <param name="searchFilterService">Service for search filtering</param>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Application.Services.Clients;

public class ClientBaseQueryService : IClientBaseQueryService
{
    private readonly DataBaseContext _context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    private const string SortDesc = "desc";
    private const string SortByFirstName = "firstname";
    private const string SortByCompany = "company";
    private const string SortByGuaranteedHours = "guaranteedhours";

    public ClientBaseQueryService(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService)
    {
        _context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
    }

    public async Task<IQueryable<Client>> BuildBaseQuery(ClientBaseFilter filter)
    {
        var startDateTime = filter.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = filter.EndDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var query = _context.Client
            .Include(c => c.Membership)
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .Where(c => c.Membership != null &&
                       c.Membership.ValidFrom <= endDateTime &&
                       (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startDateTime))
            .AsQueryable();

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);
        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);
        query = ApplyTypeFilter(query, filter.ShowEmployees, filter.ShowExtern);
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.IndividualSort, filter.StartDate);

        return query;
    }

    private static IQueryable<Client> ApplyTypeFilter(IQueryable<Client> query, bool showEmployees, bool showExtern)
    {
        if (showEmployees && showExtern)
            return query;

        if (!showEmployees && !showExtern)
            return query.Where(c => false);

        if (!showEmployees)
            return query.Where(c => c.Type == EntityTypeEnum.ExternEmp);

        return query.Where(c => c.Type == EntityTypeEnum.Employee);
    }

    private static IQueryable<Client> ApplySorting(
        IQueryable<Client> query,
        string orderBy,
        string sortOrder,
        bool individualSort,
        DateOnly refDate)
    {
        if (individualSort)
            return query.OrderBy(c => c.Name).ThenBy(c => c.FirstName);

        var isDescending = sortOrder.Equals(SortDesc, StringComparison.OrdinalIgnoreCase);

        return orderBy.ToLowerInvariant() switch
        {
            SortByFirstName => isDescending
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.Name),

            SortByCompany => isDescending
                ? query.OrderByDescending(c => c.Company).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.Company).ThenBy(c => c.Name),

            SortByGuaranteedHours => isDescending
                ? query.OrderByDescending(c => c.ClientContracts
                    .Where(cc => cc.FromDate <= refDate &&
                                 (cc.UntilDate == null || cc.UntilDate >= refDate))
                    .OrderByDescending(cc => cc.FromDate)
                    .Select(cc => cc.Contract.GuaranteedHours)
                    .FirstOrDefault())
                  .ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.ClientContracts
                    .Where(cc => cc.FromDate <= refDate &&
                                 (cc.UntilDate == null || cc.UntilDate >= refDate))
                    .OrderByDescending(cc => cc.FromDate)
                    .Select(cc => cc.Contract.GuaranteedHours)
                    .FirstOrDefault())
                  .ThenBy(c => c.Name),

            _ => isDescending
                ? query.OrderByDescending(c => c.Name).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.Name).ThenBy(c => c.FirstName)
        };
    }
}
