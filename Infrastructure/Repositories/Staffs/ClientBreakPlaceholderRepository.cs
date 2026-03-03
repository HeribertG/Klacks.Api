// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientBreakPlaceholderRepository : IClientBreakPlaceholderRepository
{
    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    public ClientBreakPlaceholderRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
    }

    public async Task<(List<Client> Clients, int TotalCount)> BreakList(BreakFilter filter)
    {
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            return await BreakListByDateRange(filter);
        }

        if (filter.CurrentYear <= 0)
        {
            return (new List<Client>(), 0);
        }

        var startOfYear = new DateTime(filter.CurrentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(filter.CurrentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = this.context.Client
            .Include(c => c.Membership)
            .Include(c => c.GroupItems)
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .AsQueryable();

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        query = query.Where(c =>
            c.Membership!.ValidFrom <= endOfYear &&
            (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startOfYear));

        query = ApplyTypeFilter(query, filter.ShowEmployees, filter.ShowExtern);

        var refDate = new DateOnly(filter.CurrentYear, 1, 1);
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.HoursSortOrder, refDate);

        var totalCount = await query.CountAsync();

        if (filter.StartRow.HasValue && filter.RowCount.HasValue)
        {
            query = query.Skip(filter.StartRow.Value).Take(filter.RowCount.Value);
        }

        var startOfYearDateOnly = new DateOnly(filter.CurrentYear, 1, 1);
        var endOfYearDateOnly = new DateOnly(filter.CurrentYear, 12, 31);

        if (filter.AbsenceIds?.Any() == true)
        {
            query = query
                .Include(c => c.BreakPlaceholders
                    .Where(b => filter.AbsenceIds.Contains(b.AbsenceId) &&
                               b.From >= startOfYear &&
                               b.From <= endOfYear)
                    .OrderBy(b => b.From)
                    .ThenBy(b => b.Until))
                .Include(c => c.Breaks
                    .Where(b => filter.AbsenceIds.Contains(b.AbsenceId) &&
                               b.CurrentDate >= startOfYearDateOnly &&
                               b.CurrentDate <= endOfYearDateOnly)
                    .OrderBy(b => b.CurrentDate));
        }

        var clients = await query.AsSingleQuery().ToListAsync();

        return (clients, totalCount);
    }

    private async Task<(List<Client> Clients, int TotalCount)> BreakListByDateRange(BreakFilter filter)
    {
        var startDateTime = filter.StartDate!.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = filter.EndDate!.Value.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var query = this.context.Client
            .Include(c => c.Membership)
            .Include(c => c.GroupItems)
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .AsQueryable();

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        query = query.Where(c =>
            c.Membership!.ValidFrom <= endDateTime &&
            (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startDateTime));

        query = ApplyTypeFilter(query, filter.ShowEmployees, filter.ShowExtern);

        var refDate = filter.StartDate!.Value;
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.HoursSortOrder, refDate);

        var totalCount = await query.CountAsync();

        query = query
            .Include(c => c.BreakPlaceholders
                .Where(bp => bp.From <= endDateTime && bp.Until >= startDateTime)
                .OrderBy(bp => bp.From)
                .ThenBy(bp => bp.Until));

        var clients = await query.AsSingleQuery().ToListAsync();

        return (clients, totalCount);
    }

    private static IQueryable<Client> ApplyTypeFilter(IQueryable<Client> query, bool showEmployees, bool showExtern)
    {
        if (showEmployees && showExtern)
        {
            return query;
        }

        if (!showEmployees && !showExtern)
        {
            return query.Where(c => false);
        }

        if (showEmployees && !showExtern)
        {
            return query.Where(c => c.Type == EntityTypeEnum.Employee);
        }

        return query.Where(c => c.Type == EntityTypeEnum.ExternEmp);
    }

    private static IQueryable<Client> ApplySorting(IQueryable<Client> query, string orderBy, string sortOrder, string? hoursSortOrder, DateOnly refDate)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        var orderedQuery = (orderBy?.ToLower()) switch
        {
            "firstname" => isDescending
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.Name),

            "company" => isDescending
                ? query.OrderByDescending(c => c.Company).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.Company).ThenBy(c => c.Name),

            "name" or _ => isDescending
                ? query.OrderByDescending(c => c.Name).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.Name).ThenBy(c => c.FirstName)
        };

        if (string.IsNullOrEmpty(hoursSortOrder))
        {
            return orderedQuery;
        }

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
