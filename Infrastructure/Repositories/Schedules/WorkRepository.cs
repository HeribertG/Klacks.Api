// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
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
        if (filter.StartDate == DateOnly.MinValue || filter.EndDate == DateOnly.MinValue)
        {
            return (new List<Client>(), 0);
        }

        var startOfYear = new DateTime(filter.StartDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(filter.EndDate.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = context.Client
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

        var periodHours = await context.ClientPeriodHours
            .Where(m => clientIds.Contains(m.ClientId)
                && m.StartDate == startDate
                && m.EndDate == endDate)
            .ToListAsync();

        var clientIdsWithPeriodHours = periodHours.Select(m => m.ClientId).ToHashSet();
        var clientIdsWithoutPeriodHours = clientIds.Where(id => !clientIdsWithPeriodHours.Contains(id)).ToList();

        var worksHours = await context.Work
            .Where(w => clientIdsWithoutPeriodHours.Contains(w.ClientId) && w.CurrentDate >= startDate && w.CurrentDate <= endDate)
            .GroupBy(w => w.ClientId)
            .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(w => w.WorkTime), TotalSurcharges = g.Sum(w => w.Surcharges) })
            .ToListAsync();

        var breaksHours = await context.Break
            .Where(b => clientIdsWithoutPeriodHours.Contains(b.ClientId) && b.CurrentDate >= startDate && b.CurrentDate <= endDate)
            .GroupBy(b => b.ClientId)
            .Select(g => new { ClientId = g.Key, TotalBreaks = g.Sum(b => b.WorkTime) })
            .ToListAsync();

        var workChanges = await context.WorkChange
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

        var contractData = await context.ClientContract
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

    public async Task<List<Work>> GetByClientAndDateRangeAsync(Guid clientId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .AsNoTracking()
            .Where(w => w.ClientId == clientId && !w.IsDeleted)
            .Where(w => w.CurrentDate >= DateOnly.FromDateTime(fromDate) && w.CurrentDate <= DateOnly.FromDateTime(toDate))
            .OrderBy(w => w.CurrentDate)
            .ThenBy(w => w.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .Where(w => !w.IsDeleted && w.CurrentDate == date && w.LockLevel < level)
            .Where(w => context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted))
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.LockLevel, level)
                .SetProperty(w => w.SealedAt, DateTime.UtcNow)
                .SetProperty(w => w.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .Where(w => !w.IsDeleted && w.CurrentDate == date && w.LockLevel == level)
            .Where(w => context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted))
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.LockLevel, WorkLockLevel.None)
                .SetProperty(w => w.SealedAt, (DateTime?)null)
                .SetProperty(w => w.SealedBy, (string?)null), cancellationToken);
    }

    public async Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .Where(w => !w.IsDeleted && w.CurrentDate >= startDate && w.CurrentDate <= endDate && w.LockLevel < level)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.LockLevel, level)
                .SetProperty(w => w.SealedAt, DateTime.UtcNow)
                .SetProperty(w => w.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .Where(w => !w.IsDeleted && w.CurrentDate >= startDate && w.CurrentDate <= endDate && w.LockLevel == level)
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.LockLevel, WorkLockLevel.None)
                .SetProperty(w => w.SealedAt, (DateTime?)null)
                .SetProperty(w => w.SealedBy, (string?)null), cancellationToken);
    }
}
