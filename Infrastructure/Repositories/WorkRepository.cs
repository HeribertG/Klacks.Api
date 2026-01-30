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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;
    private readonly IWorkMacroService _workMacroService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkRepository(
        DataBaseContext context,
        ILogger<Work> logger,
        IUnitOfWork unitOfWork,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService,
        IWorkMacroService workMacroService,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor)
        : base(context, logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
        _workMacroService = workMacroService;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task Add(Work work)
    {
        await _workMacroService.ProcessWorkMacroAsync(work);
        await base.Add(work);
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
        await RecalculatePeriodHoursAsync(work.ClientId, periodStart, periodEnd);
    }

    public override async Task<Work?> Put(Work work)
    {
        await _workMacroService.ProcessWorkMacroAsync(work);
        var result = await base.Put(work);
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
        await RecalculatePeriodHoursAsync(work.ClientId, periodStart, periodEnd);
        return result;
    }

    public override async Task<Work?> Delete(Guid id)
    {
        var work = await base.Get(id);
        var result = await base.Delete(id);
        if (work != null)
        {
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
            await RecalculatePeriodHoursAsync(work.ClientId, periodStart, periodEnd);
        }
        return result;
    }

    public async Task<(Work Work, PeriodHoursResource PeriodHours)> AddWithPeriodHours(Work work, DateOnly periodStart, DateOnly periodEnd)
    {
        await _workMacroService.ProcessWorkMacroAsync(work);
        await base.Add(work);
        await _unitOfWork.CompleteAsync();
        var periodHours = await RecalculateAndGetPeriodHoursAsync(work.ClientId, periodStart, periodEnd);
        return (work, periodHours);
    }

    public async Task<(Work? Work, PeriodHoursResource? PeriodHours)> PutWithPeriodHours(Work work, DateOnly periodStart, DateOnly periodEnd)
    {
        await _workMacroService.ProcessWorkMacroAsync(work);
        var result = await base.Put(work);
        if (result == null) return (null, null);
        await _unitOfWork.CompleteAsync();
        var periodHours = await RecalculateAndGetPeriodHoursAsync(work.ClientId, periodStart, periodEnd);
        return (result, periodHours);
    }

    public async Task<(Work? Work, PeriodHoursResource? PeriodHours)> DeleteWithPeriodHours(Guid id, DateOnly periodStart, DateOnly periodEnd)
    {
        var work = await base.Get(id);
        var result = await base.Delete(id);
        if (work == null) return (null, null);
        await _unitOfWork.CompleteAsync();
        var periodHours = await RecalculateAndGetPeriodHoursAsync(work.ClientId, periodStart, periodEnd);
        return (result, periodHours);
    }

    private async Task RecalculatePeriodHoursAsync(Guid clientId, DateOnly periodStart, DateOnly periodEnd)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        await _periodHoursService.RecalculateAndNotifyAsync(
            clientId,
            periodStart,
            periodEnd,
            connectionId);
    }

    private async Task<PeriodHoursResource> RecalculateAndGetPeriodHoursAsync(Guid clientId, DateOnly periodStart, DateOnly periodEnd)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        return await _periodHoursService.RecalculateAndNotifyAsync(
            clientId,
            periodStart,
            periodEnd,
            connectionId);
    }

    public async Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter)
    {
        if (filter.StartDate == DateOnly.MinValue || filter.EndDate == DateOnly.MinValue)
        {
            return (new List<Client>(), 0);
        }

        var startOfYear = new DateTime(filter.StartDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(filter.EndDate.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = _context.Client
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .Include(c => c.Membership)
            .Where(c => c.Membership != null &&
                        c.Membership.ValidFrom <= endOfYear &&
                        (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startOfYear))
            .AsQueryable();

        query = query.Include(c => c.Works.Where(w => w.CurrentDate >= filter.StartDate && w.CurrentDate <= filter.EndDate));
        query = query.Include(c => c.ClientContracts.Where(cc => !cc.IsDeleted && cc.IsActive))
            .ThenInclude(cc => cc.Contract);

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        query = ApplyTypeFilter(query, filter.ShowEmployees, filter.ShowExtern);

        query = ApplySorting(query, filter.OrderBy, filter.SortOrder, filter.HoursSortOrder, filter.StartDate);

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

    public async Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, PeriodHoursResource>();
        }

        var result = new Dictionary<Guid, PeriodHoursResource>();

        var periodHours = await _context.ClientPeriodHours
            .Where(m => clientIds.Contains(m.ClientId)
                && m.StartDate == startDate
                && m.EndDate == endDate)
            .ToListAsync();

        var clientIdsWithPeriodHours = periodHours.Select(m => m.ClientId).ToHashSet();
        var clientIdsWithoutPeriodHours = clientIds.Where(id => !clientIdsWithPeriodHours.Contains(id)).ToList();

        var worksHours = await _context.Work
            .Where(w => clientIdsWithoutPeriodHours.Contains(w.ClientId) && w.CurrentDate >= startDate && w.CurrentDate <= endDate)
            .GroupBy(w => w.ClientId)
            .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(w => w.WorkTime), TotalSurcharges = g.Sum(w => w.Surcharges) })
            .ToListAsync();

        var breaksHours = await _context.Break
            .Where(b => clientIdsWithoutPeriodHours.Contains(b.ClientId) && b.CurrentDate >= startDate && b.CurrentDate <= endDate)
            .GroupBy(b => b.ClientId)
            .Select(g => new { ClientId = g.Key, TotalBreaks = g.Sum(b => b.WorkTime) })
            .ToListAsync();

        var workChanges = await _context.WorkChange
            .Where(wc => clientIdsWithoutPeriodHours.Contains(wc.Work!.ClientId) && wc.Work.CurrentDate >= startDate && wc.Work.CurrentDate <= endDate)
            .Select(wc => new
            {
                wc.Work!.ClientId,
                wc.ChangeTime,
                wc.Type,
                wc.ToInvoice,
                wc.ReplaceClientId,
                OriginalClientId = wc.Work.ClientId
            })
            .ToListAsync();

        var worksHoursDict = worksHours.ToDictionary(x => x.ClientId, x => (Hours: x.TotalHours, Surcharges: x.TotalSurcharges));
        var breaksHoursDict = breaksHours.ToDictionary(x => x.ClientId, x => x.TotalBreaks);

        var contractData = await _context.ClientContract
            .Where(cc => clientIds.Contains(cc.ClientId) && cc.FromDate <= startDate && (cc.UntilDate == null || cc.UntilDate >= startDate))
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
                GuaranteedHours = guaranteedHours ?? 0m
            };
        }

        foreach (var clientId in clientIdsWithoutPeriodHours)
        {
            var workData = worksHoursDict.TryGetValue(clientId, out var wd) ? wd : (Hours: 0m, Surcharges: 0m);
            var breaks = breaksHoursDict.TryGetValue(clientId, out var b) ? b : 0m;

            var workChangeHours = 0m;
            var workChangeSurcharges = 0m;

            foreach (var wc in workChanges)
            {
                if (wc.ToInvoice == true && wc.OriginalClientId == clientId)
                {
                    workChangeSurcharges += wc.ChangeTime;
                }

                var isOriginalClient = wc.OriginalClientId == clientId;
                var isReplacementClient = wc.ReplaceClientId == clientId;

                if (wc.Type == WorkChangeType.CorrectionEnd || wc.Type == WorkChangeType.CorrectionStart)
                {
                    if (isOriginalClient) workChangeHours += wc.ChangeTime;
                }
                else if (wc.Type == WorkChangeType.ReplacementStart || wc.Type == WorkChangeType.ReplacementEnd)
                {
                    if (isOriginalClient) workChangeHours -= wc.ChangeTime;
                    if (isReplacementClient) workChangeHours += wc.ChangeTime;
                }
            }

            var guaranteedHours = contractByClient.TryGetValue(clientId, out var contract)
                ? contract.GuaranteedHours ?? 0m
                : 0m;

            result[clientId] = new PeriodHoursResource
            {
                Hours = workData.Hours + breaks + workChangeHours,
                Surcharges = workData.Surcharges + workChangeSurcharges,
                GuaranteedHours = guaranteedHours
            };
        }

        return result;
    }
}
