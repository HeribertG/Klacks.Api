using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ClientWorkRepository : IClientWorkRepository
{
    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    public ClientWorkRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
    }

    public async Task<List<Client>> WorkList(WorkFilter filter)
    {
        if (filter.CurrentYear <= 0 || filter.CurrentMonth <= 0)
        {
            return new List<Client>();
        }

        var startOfYear = new DateTime(filter.CurrentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(filter.CurrentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = this.context.Client
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .Include(c => c.Membership)
            .Where(c => c.Membership != null &&
                        c.Membership.ValidFrom <= endOfYear &&
                        (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startOfYear))
            .AsQueryable();

        var startDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, 1)
            .AddDays(-filter.DayVisibleBeforeMonth);
        var endDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, DateTime.DaysInMonth(filter.CurrentYear, filter.CurrentMonth))
            .AddDays(filter.DayVisibleAfterMonth);

        query = query.Include(c => c.Works.Where(w => (w.From >= startDate && w.From <= endDate) ||
                                                     (w.Until >= startDate && w.Until <= endDate) ||
                                                     (w.From <= startDate && w.Until >= endDate)));

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        return await query.ToListAsync();
    }
}