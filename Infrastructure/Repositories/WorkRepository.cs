using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Schedules;
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

        query = ApplyTypeFilter(query, filter.ShowEmployees, filter.ShowExtern);

        var refDate = new DateOnly(filter.CurrentYear, filter.CurrentMonth, 1);
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, refDate);

        return await query.ToListAsync();
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

    private static IQueryable<Client> ApplySorting(IQueryable<Client> query, string orderBy, string sortOrder, DateOnly refDate)
    {
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return orderBy.ToLowerInvariant() switch
        {
            "firstname" => isDescending
                ? query.OrderByDescending(c => c.FirstName)
                : query.OrderBy(c => c.FirstName),
            "company" => isDescending
                ? query.OrderByDescending(c => c.Company)
                : query.OrderBy(c => c.Company),
            "type" => isDescending
                ? query.OrderByDescending(c => c.Type)
                : query.OrderBy(c => c.Type),
            "hours" => isDescending
                ? query.OrderByDescending(c => c.ClientContracts
                    .Where(cc => cc.FromDate <= refDate && (cc.UntilDate == null || cc.UntilDate >= refDate))
                    .OrderByDescending(cc => cc.FromDate)
                    .Select(cc => cc.Contract.GuaranteedHoursPerMonth)
                    .FirstOrDefault())
                : query.OrderBy(c => c.ClientContracts
                    .Where(cc => cc.FromDate <= refDate && (cc.UntilDate == null || cc.UntilDate >= refDate))
                    .OrderByDescending(cc => cc.FromDate)
                    .Select(cc => cc.Contract.GuaranteedHoursPerMonth)
                    .FirstOrDefault()),
            _ => isDescending
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name)
        };
    }

    public async Task<Dictionary<Guid, MonthlyHoursResource>> GetMonthlyHoursForClients(List<Guid> clientIds, int year, int month)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, MonthlyHoursResource>();
        }

        var result = new Dictionary<Guid, MonthlyHoursResource>();

        var monthlyHours = await _context.MonthlyClientHours
            .Where(m => clientIds.Contains(m.ClientId) && m.Year == year && m.Month == month)
            .ToListAsync();

        var clientIdsWithMonthlyHours = monthlyHours.Select(m => m.ClientId).ToHashSet();
        var clientIdsWithoutMonthlyHours = clientIds.Where(id => !clientIdsWithMonthlyHours.Contains(id)).ToList();

        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

        var worksHours = await _context.Work
            .Where(w => clientIdsWithoutMonthlyHours.Contains(w.ClientId) && w.CurrentDate >= startOfMonth && w.CurrentDate <= endOfMonth)
            .GroupBy(w => w.ClientId)
            .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(w => w.WorkTime) })
            .ToListAsync();

        var worksHoursDict = worksHours.ToDictionary(x => x.ClientId, x => x.TotalHours);

        var refDate = new DateOnly(year, month, 1);
        var contractData = await _context.ClientContract
            .Where(cc => clientIds.Contains(cc.ClientId) && cc.FromDate <= refDate && (cc.UntilDate == null || cc.UntilDate >= refDate))
            .Include(cc => cc.Contract)
            .ToListAsync();

        var contractByClient = contractData
            .GroupBy(cc => cc.ClientId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(cc => cc.FromDate).First().Contract);

        foreach (var mh in monthlyHours)
        {
            var guaranteedHours = contractByClient.TryGetValue(mh.ClientId, out var contract)
                ? contract.GuaranteedHoursPerMonth
                : 0m;

            result[mh.ClientId] = new MonthlyHoursResource
            {
                Hours = mh.Hours,
                Surcharges = mh.Surcharges,
                GuaranteedHoursPerMonth = guaranteedHours
            };
        }

        foreach (var clientId in clientIdsWithoutMonthlyHours)
        {
            var hours = worksHoursDict.TryGetValue(clientId, out var h) ? h : 0m;
            var guaranteedHours = contractByClient.TryGetValue(clientId, out var contract)
                ? contract.GuaranteedHoursPerMonth
                : 0m;

            result[clientId] = new MonthlyHoursResource
            {
                Hours = hours,
                Surcharges = 0m,
                GuaranteedHoursPerMonth = guaranteedHours
            };
        }

        return result;
    }
}
