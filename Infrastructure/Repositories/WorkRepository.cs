using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
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
    private readonly IWorkMacroService _workMacroService;

    public WorkRepository(
        DataBaseContext context,
        ILogger<Work> logger,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService,
        IWorkMacroService workMacroService)
        : base(context, logger)
    {
        _context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
        _workMacroService = workMacroService;
    }

    public override async Task Add(Work work)
    {
        await _workMacroService.ProcessWorkMacroAsync(work);
        await base.Add(work);
    }

    public override async Task<Work?> Put(Work work)
    {
        await _workMacroService.ProcessWorkMacroAsync(work);
        return await base.Put(work);
    }

    public async Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter)
    {
        if (filter.CurrentYear <= 0 || filter.CurrentMonth <= 0)
        {
            return (new List<Client>(), 0);
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

        var startDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddDays(-filter.DayVisibleBeforeMonth);
        var endDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, DateTime.DaysInMonth(filter.CurrentYear, filter.CurrentMonth), 23, 59, 59, DateTimeKind.Utc)
            .AddDays(filter.DayVisibleAfterMonth);

        query = query.Include(c => c.Works.Where(w => w.CurrentDate >= startDate && w.CurrentDate <= endDate));

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        query = ApplyTypeFilter(query, filter.ShowEmployees, filter.ShowExtern);

        var refDate = new DateOnly(filter.CurrentYear, filter.CurrentMonth, 1);
        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.HoursSortOrder, refDate);

        var totalCount = await query.CountAsync();

        var clients = await query
            .Skip(filter.StartRow)
            .Take(filter.RowCount)
            .ToListAsync();

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
        var isDescending = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase);

        var orderedQuery = orderBy.ToLowerInvariant() switch
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
            _ => isDescending
                ? query.OrderByDescending(c => c.Name)
                : query.OrderBy(c => c.Name)
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

    public async Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, int year, int month)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, PeriodHoursResource>();
        }

        var result = new Dictionary<Guid, PeriodHoursResource>();

        var periodHours = await _context.ClientPeriodHours
            .Where(m => clientIds.Contains(m.ClientId) && m.Year == year && m.Month == month && m.PaymentInterval == Domain.Enums.PaymentInterval.Monthly)
            .ToListAsync();

        var clientIdsWithPeriodHours = periodHours.Select(m => m.ClientId).ToHashSet();
        var clientIdsWithoutPeriodHours = clientIds.Where(id => !clientIdsWithPeriodHours.Contains(id)).ToList();

        var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

        var worksHours = await _context.Work
            .Where(w => clientIdsWithoutPeriodHours.Contains(w.ClientId) && w.CurrentDate >= startOfMonth && w.CurrentDate <= endOfMonth)
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

        foreach (var ph in periodHours)
        {
            var guaranteedHours = contractByClient.TryGetValue(ph.ClientId, out var contract)
                ? contract.GuaranteedHours
                : 0m;

            result[ph.ClientId] = new PeriodHoursResource
            {
                Hours = ph.Hours,
                Surcharges = ph.Surcharges,
                GuaranteedHours = guaranteedHours
            };
        }

        foreach (var clientId in clientIdsWithoutPeriodHours)
        {
            var hours = worksHoursDict.TryGetValue(clientId, out var h) ? h : 0m;
            var guaranteedHours = contractByClient.TryGetValue(clientId, out var contract)
                ? contract.GuaranteedHours
                : 0m;

            result[clientId] = new PeriodHoursResource
            {
                Hours = hours,
                Surcharges = 0m,
                GuaranteedHours = guaranteedHours
            };
        }

        return result;
    }
}
