// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientBreakPlaceholderRepository : IClientBreakPlaceholderRepository
{
    private readonly DataBaseContext context;
    private readonly IClientBaseQueryService _baseQueryService;

    public ClientBreakPlaceholderRepository(
        DataBaseContext context,
        IClientBaseQueryService baseQueryService)
    {
        this.context = context;
        _baseQueryService = baseQueryService;
    }

    public async Task<(List<Client> Clients, int TotalCount)> BreakList(BreakFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            return await BreakListByDateRange(filter, cancellationToken);
        }

        if (filter.CurrentYear <= 0)
        {
            return (new List<Client>(), 0);
        }

        var startOfYear = new DateOnly(filter.CurrentYear, 1, 1);
        var endOfYear = new DateOnly(filter.CurrentYear, 12, 31);

        var baseFilter = new ClientBaseFilter
        {
            StartDate = startOfYear,
            EndDate = endOfYear,
            SearchString = filter.SearchString,
            SelectedGroup = filter.SelectedGroup,
            ShowEmployees = filter.ShowEmployees,
            ShowExtern = filter.ShowExtern,
            OrderBy = filter.OrderBy,
            SortOrder = filter.SortOrder,
            IndividualSort = filter.IndividualSort,
        };

        var query = await _baseQueryService.BuildBaseQuery(baseFilter);

        query = query.Include(c => c.GroupItems);

        var totalCount = await query.CountAsync(cancellationToken);

        if (filter.StartRow.HasValue && filter.RowCount.HasValue)
        {
            query = query.Skip(filter.StartRow.Value).Take(filter.RowCount.Value);
        }

        var startOfYearDateTime = new DateTime(filter.CurrentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYearDateTime = new DateTime(filter.CurrentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        if (filter.AbsenceIds?.Any() == true)
        {
            query = query
                .Include(c => c.BreakPlaceholders
                    .Where(b => filter.AbsenceIds.Contains(b.AbsenceId) &&
                               b.From >= startOfYearDateTime &&
                               b.From <= endOfYearDateTime)
                    .OrderBy(b => b.From)
                    .ThenBy(b => b.Until))
                .Include(c => c.Breaks
                    .Where(b => filter.AbsenceIds.Contains(b.AbsenceId) &&
                               b.CurrentDate >= startOfYear &&
                               b.CurrentDate <= endOfYear)
                    .OrderBy(b => b.CurrentDate));
        }

        var clients = await query.AsSingleQuery().ToListAsync(cancellationToken);

        return (clients, totalCount);
    }

    private async Task<(List<Client> Clients, int TotalCount)> BreakListByDateRange(BreakFilter filter, CancellationToken cancellationToken = default)
    {
        var startDate = filter.StartDate!.Value;
        var endDate = filter.EndDate!.Value;
        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var baseFilter = new ClientBaseFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            SearchString = filter.SearchString,
            SelectedGroup = filter.SelectedGroup,
            ShowEmployees = filter.ShowEmployees,
            ShowExtern = filter.ShowExtern,
            OrderBy = filter.OrderBy,
            SortOrder = filter.SortOrder,
            IndividualSort = filter.IndividualSort,
        };

        var query = await _baseQueryService.BuildBaseQuery(baseFilter);

        query = query.Include(c => c.GroupItems);

        var totalCount = await query.CountAsync(cancellationToken);

        query = query
            .Include(c => c.BreakPlaceholders
                .Where(bp => bp.From <= endDateTime && bp.Until >= startDateTime)
                .OrderBy(bp => bp.From)
                .ThenBy(bp => bp.Until));

        var clients = await query.AsSingleQuery().ToListAsync(cancellationToken);

        return (clients, totalCount);
    }
}
