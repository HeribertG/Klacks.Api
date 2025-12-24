using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
    private readonly DataBaseContext _context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    public WorkRepository(
        DataBaseContext context,
        ILogger<Work> logger,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService)
        : base(context, logger)
    {
        _context = context;
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

        var query = _context.Client
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

        query = query.Include(c => c.Works.Where(w => w.CurrentDate >= startDate && w.CurrentDate <= endDate));

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        return await query.ToListAsync();
    }
}
