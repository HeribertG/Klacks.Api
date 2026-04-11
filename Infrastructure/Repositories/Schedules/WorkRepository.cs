// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
    private readonly IClientBaseQueryService _baseQueryService;
    private readonly IWorkMacroService _workMacroService;
    private readonly IClientContractDataProvider _contractDataProvider;

    public WorkRepository(
        DataBaseContext context,
        ILogger<Work> logger,
        IClientBaseQueryService baseQueryService,
        IWorkMacroService workMacroService,
        IClientContractDataProvider contractDataProvider)
        : base(context, logger)
    {
        _baseQueryService = baseQueryService;
        _workMacroService = workMacroService;
        _contractDataProvider = contractDataProvider;
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

    public async Task<(List<Client> Clients, int TotalCount)> WorkList(WorkFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.StartDate == DateOnly.MinValue || filter.EndDate == DateOnly.MinValue)
        {
            return (new List<Client>(), 0);
        }

        var baseFilter = new ClientBaseFilter
        {
            StartDate = filter.StartDate,
            EndDate = filter.EndDate,
            SearchString = filter.SearchString,
            SelectedGroup = filter.SelectedGroup,
            ShowEmployees = filter.ShowEmployees,
            ShowExtern = filter.ShowExtern,
            OrderBy = filter.OrderBy,
            SortOrder = filter.SortOrder,
            HoursSortOrder = filter.HoursSortOrder,
        };

        var query = await _baseQueryService.BuildBaseQuery(baseFilter);

        query = query.Include(c => c.Works.Where(w => w.CurrentDate >= filter.StartDate && w.CurrentDate <= filter.EndDate));
        query = query.Include(c => c.ClientContracts.Where(cc => !cc.IsDeleted && cc.IsActive))
            .ThenInclude(cc => cc.Contract);

        var totalCount = await query.CountAsync(cancellationToken);

        var clients = await query
            .Skip(filter.StartRow)
            .Take(filter.RowCount)
            .ToListAsync(cancellationToken);

        return (clients, totalCount);
    }

    public async Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursForClients(List<Guid> clientIds, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
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
            .ToListAsync(cancellationToken);

        var clientIdsWithPeriodHours = periodHours.Select(m => m.ClientId).ToHashSet();
        var clientIdsWithoutPeriodHours = clientIds.Where(id => !clientIdsWithPeriodHours.Contains(id)).ToList();

        var worksHours = await context.Work
            .Where(w => clientIdsWithoutPeriodHours.Contains(w.ClientId) && w.CurrentDate >= startDate && w.CurrentDate <= endDate)
            .GroupBy(w => w.ClientId)
            .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(w => w.WorkTime), TotalSurcharges = g.Sum(w => w.Surcharges) })
            .ToListAsync(cancellationToken);

        var breaksHours = await context.Break
            .Where(b => clientIdsWithoutPeriodHours.Contains(b.ClientId) && b.CurrentDate >= startDate && b.CurrentDate <= endDate)
            .GroupBy(b => b.ClientId)
            .Select(g => new { ClientId = g.Key, TotalBreaks = g.Sum(b => b.WorkTime) })
            .ToListAsync(cancellationToken);

        var workChanges = await context.WorkChange
            .Where(wc => clientIdsWithoutPeriodHours.Contains(wc.Work!.ClientId) && wc.Work.CurrentDate >= startDate && wc.Work.CurrentDate <= endDate)
            .Select(wc => new WorkChangeEntry(wc.Work!.ClientId, wc.ChangeTime, wc.Type, wc.ToInvoice, wc.ReplaceClientId, wc.Work.ClientId))
            .ToListAsync(cancellationToken);

        var worksHoursDict = worksHours.ToDictionary(x => x.ClientId, x => (Hours: x.TotalHours, Surcharges: x.TotalSurcharges));
        var breaksHoursDict = breaksHours.ToDictionary(x => x.ClientId, x => x.TotalBreaks);

        var effectiveDataByClient = await _contractDataProvider.GetEffectiveContractDataForClientsAsync(clientIds, startDate);

        foreach (var ph in periodHours)
        {
            var guaranteedHours = effectiveDataByClient.TryGetValue(ph.ClientId, out var data)
                ? data.GuaranteedHours
                : 0m;

            result[ph.ClientId] = new PeriodHoursResource
            {
                Hours = ph.Hours,
                Surcharges = ph.Surcharges,
                GuaranteedHours = guaranteedHours
            };
        }

        var fallback = BuildFallbackPeriodHours(clientIdsWithoutPeriodHours, worksHoursDict, breaksHoursDict, workChanges, effectiveDataByClient);

        foreach (var kvp in fallback)
        {
            result[kvp.Key] = kvp.Value;
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

    public async Task<int> SealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .Where(w => !w.IsDeleted && w.CurrentDate >= startDate && w.CurrentDate <= endDate && w.LockLevel < level)
            .Where(w => context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted))
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.LockLevel, level)
                .SetProperty(w => w.SealedAt, DateTime.UtcNow)
                .SetProperty(w => w.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByPeriodAndGroup(DateOnly startDate, DateOnly endDate, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await context.Work
            .Where(w => !w.IsDeleted && w.CurrentDate >= startDate && w.CurrentDate <= endDate && w.LockLevel == level)
            .Where(w => context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted))
            .ExecuteUpdateAsync(s => s
                .SetProperty(w => w.LockLevel, WorkLockLevel.None)
                .SetProperty(w => w.SealedAt, (DateTime?)null)
                .SetProperty(w => w.SealedBy, (string?)null), cancellationToken);
    }

    private static Dictionary<Guid, PeriodHoursResource> BuildFallbackPeriodHours(
        List<Guid> clientIds,
        Dictionary<Guid, (decimal Hours, decimal Surcharges)> worksHoursDict,
        Dictionary<Guid, decimal> breaksHoursDict,
        List<WorkChangeEntry> workChanges,
        Dictionary<Guid, EffectiveContractData> effectiveDataByClient)
    {
        var result = new Dictionary<Guid, PeriodHoursResource>();

        foreach (var clientId in clientIds)
        {
            var workData = worksHoursDict.TryGetValue(clientId, out var wd) ? wd : (Hours: 0m, Surcharges: 0m);
            var breaks = breaksHoursDict.TryGetValue(clientId, out var b) ? b : 0m;

            var (workChangeHours, workChangeSurcharges) = CalculateWorkChangeAdjustments(workChanges, clientId);

            var guaranteedHours = effectiveDataByClient.TryGetValue(clientId, out var data)
                ? data.GuaranteedHours
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

    private static (decimal hours, decimal surcharges) CalculateWorkChangeAdjustments(
        IEnumerable<WorkChangeEntry> workChanges, Guid clientId)
    {
        var hours = 0m;
        var surcharges = 0m;

        foreach (var wc in workChanges)
        {
            if (wc.ToInvoice == true && wc.OriginalClientId == clientId)
            {
                surcharges += wc.ChangeTime;
            }

            var isOriginalClient = wc.OriginalClientId == clientId;
            var isReplacementClient = wc.ReplaceClientId == clientId;

            if (wc.Type == WorkChangeType.CorrectionEnd || wc.Type == WorkChangeType.CorrectionStart)
            {
                if (isOriginalClient) hours += wc.ChangeTime;
            }
            else if (wc.Type == WorkChangeType.ReplacementStart || wc.Type == WorkChangeType.ReplacementEnd)
            {
                if (isOriginalClient) hours -= wc.ChangeTime;
                if (isReplacementClient) hours += wc.ChangeTime;
            }
        }

        return (hours, surcharges);
    }

    private record WorkChangeEntry(Guid ClientId, decimal ChangeTime, WorkChangeType Type, bool? ToInvoice, Guid? ReplaceClientId, Guid OriginalClientId);
}
