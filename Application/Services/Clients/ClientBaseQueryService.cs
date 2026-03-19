// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Zentraler Service für Client-Base-Queries.
/// Vereinheitlicht Membership-, Gruppen-, Such-, Typ-Filter und Sorting für alle Client-Listen.
/// Gibt IQueryable zurück — Count, Paging und Includes werden vom Aufrufer hinzugefügt.
/// </summary>
/// <param name="context">Datenbankkontext</param>
/// <param name="groupFilterService">Service für Gruppen-Filterung</param>
/// <param name="searchFilterService">Service für Such-Filterung</param>
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
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.HoursSortOrder, filter.StartDate);

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
        string? hoursSortOrder,
        DateOnly refDate)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        var orderedQuery = orderBy.ToLowerInvariant() switch
        {
            "firstname" => isDescending
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.Name),
            "company" => isDescending
                ? query.OrderByDescending(c => c.Company).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.Company).ThenBy(c => c.Name),
            _ => isDescending
                ? query.OrderByDescending(c => c.Name).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.Name).ThenBy(c => c.FirstName)
        };

        if (string.IsNullOrEmpty(hoursSortOrder))
            return orderedQuery;

        var hoursDescending = hoursSortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);
        return hoursDescending
            ? orderedQuery.ThenByDescending(c => c.ClientContracts
                .Where(cc => cc.FromDate <= refDate && (cc.UntilDate == null || cc.UntilDate >= refDate))
                .OrderByDescending(cc => cc.FromDate)
                .Select(cc => cc.Contract.GuaranteedHours)
                .FirstOrDefault())
            : orderedQuery.ThenBy(c => c.ClientContracts
                .Where(cc => cc.FromDate <= refDate && (cc.UntilDate == null || cc.UntilDate >= refDate))
                .OrderByDescending(cc => cc.FromDate)
                .Select(cc => cc.Contract.GuaranteedHours)
                .FirstOrDefault());
    }
}
