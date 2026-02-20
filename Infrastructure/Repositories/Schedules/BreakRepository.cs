using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class BreakRepository : BaseRepository<Break>, IBreakRepository
{
    private readonly DataBaseContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IBreakMacroService _breakMacroService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly IScheduleTimelineService _timelineService;

    public BreakRepository(
        DataBaseContext context,
        ILogger<Break> logger,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IBreakMacroService breakMacroService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        IScheduleTimelineService timelineService)
        : base(context, logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _breakMacroService = breakMacroService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
        _timelineService = timelineService;
    }

    public override async Task Add(Break entity)
    {
        await _breakMacroService.ProcessBreakMacroAsync(entity);
        await base.Add(entity);
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(entity.CurrentDate);
        await RecalculatePeriodHoursAsync(entity.ClientId, periodStart, periodEnd);
    }

    public override async Task<Break?> Put(Break entity)
    {
        await _breakMacroService.ProcessBreakMacroAsync(entity);
        var result = await base.Put(entity);
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(entity.CurrentDate);
        await RecalculatePeriodHoursAsync(entity.ClientId, periodStart, periodEnd);
        return result;
    }

    public override async Task<Break?> Delete(Guid id)
    {
        var entity = await base.Get(id);
        var result = await base.Delete(id);
        if (entity != null)
        {
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(entity.CurrentDate);
            await RecalculatePeriodHoursAsync(entity.ClientId, periodStart, periodEnd);
        }
        return result;
    }

    public async Task<(Break Break, PeriodHoursResource PeriodHours)> AddWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd)
    {
        await _breakMacroService.ProcessBreakMacroAsync(breakEntry);
        await base.Add(breakEntry);
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(breakEntry.ClientId, breakEntry.CurrentDate);
        _timelineService.QueueCheck(breakEntry.ClientId, breakEntry.CurrentDate);
        var periodHours = await RecalculateAndGetPeriodHoursAsync(breakEntry.ClientId, periodStart, periodEnd);
        return (breakEntry, periodHours);
    }

    public async Task<(Break? Break, PeriodHoursResource? PeriodHours)> PutWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd)
    {
        await _breakMacroService.ProcessBreakMacroAsync(breakEntry);
        var result = await base.Put(breakEntry);
        if (result == null) return (null, null);
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(breakEntry.ClientId, breakEntry.CurrentDate);
        _timelineService.QueueCheck(breakEntry.ClientId, breakEntry.CurrentDate);
        var periodHours = await RecalculateAndGetPeriodHoursAsync(breakEntry.ClientId, periodStart, periodEnd);
        return (result, periodHours);
    }

    public async Task<(Break? Break, PeriodHoursResource? PeriodHours)> DeleteWithPeriodHours(Guid id, DateOnly periodStart, DateOnly periodEnd)
    {
        var breakEntry = await base.Get(id);
        var result = await base.Delete(id);
        if (breakEntry == null) return (null, null);
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(breakEntry.ClientId, breakEntry.CurrentDate);
        _timelineService.QueueCheck(breakEntry.ClientId, breakEntry.CurrentDate);
        var periodHours = await RecalculateAndGetPeriodHoursAsync(breakEntry.ClientId, periodStart, periodEnd);
        return (result, periodHours);
    }

    public async Task<List<Break>> BulkAddWithTracking(List<Break> breaks)
    {
        foreach (var breakEntry in breaks)
        {
            await _breakMacroService.ProcessBreakMacroAsync(breakEntry);
            await base.Add(breakEntry);
        }
        await _unitOfWork.CompleteAsync();
        foreach (var breakEntry in breaks)
        {
            await _scheduleChangeTracker.TrackChangeAsync(breakEntry.ClientId, breakEntry.CurrentDate);
            _timelineService.QueueCheck(breakEntry.ClientId, breakEntry.CurrentDate);
        }
        return breaks;
    }

    public async Task<List<Break>> BulkDeleteWithTracking(List<Guid> breakIds)
    {
        var deletedBreaks = new List<Break>();
        foreach (var breakId in breakIds)
        {
            var breakEntry = await base.Get(breakId);
            if (breakEntry == null) continue;
            deletedBreaks.Add(breakEntry);
            await base.Delete(breakId);
        }
        await _unitOfWork.CompleteAsync();
        foreach (var breakEntry in deletedBreaks)
        {
            await _scheduleChangeTracker.TrackChangeAsync(breakEntry.ClientId, breakEntry.CurrentDate);
            _timelineService.QueueCheck(breakEntry.ClientId, breakEntry.CurrentDate);
        }
        return deletedBreaks;
    }

    private async Task RecalculatePeriodHoursAsync(Guid clientId, DateOnly periodStart, DateOnly periodEnd)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        await _periodHoursService.RecalculateAndNotifyAsync(
            clientId,
            periodStart,
            periodEnd,
            connectionId);
    }

    private async Task<PeriodHoursResource> RecalculateAndGetPeriodHoursAsync(Guid clientId, DateOnly periodStart, DateOnly periodEnd)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        return await _periodHoursService.RecalculateAndNotifyAsync(
            clientId,
            periodStart,
            periodEnd,
            connectionId);
    }

    public async Task<int> SealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate == date && b.LockLevel < level)
            .Where(b => _context.Work.Any(w => !w.IsDeleted && w.ClientId == b.ClientId && w.CurrentDate == date
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, level)
                .SetProperty(b => b.SealedAt, DateTime.UtcNow)
                .SetProperty(b => b.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByDayAndGroup(DateOnly date, Guid groupId, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate == date && b.LockLevel == level)
            .Where(b => _context.Work.Any(w => !w.IsDeleted && w.ClientId == b.ClientId && w.CurrentDate == date
                && _context.GroupItem.Any(gi => gi.ShiftId == w.ShiftId && gi.GroupId == groupId && !gi.IsDeleted)))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, WorkLockLevel.None)
                .SetProperty(b => b.SealedAt, (DateTime?)null)
                .SetProperty(b => b.SealedBy, (string?)null), cancellationToken);
    }

    public async Task<int> SealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, string sealedBy, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate && b.LockLevel < level)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, level)
                .SetProperty(b => b.SealedAt, DateTime.UtcNow)
                .SetProperty(b => b.SealedBy, sealedBy), cancellationToken);
    }

    public async Task<int> UnsealByPeriod(DateOnly startDate, DateOnly endDate, WorkLockLevel level, CancellationToken cancellationToken = default)
    {
        return await _context.Break
            .Where(b => !b.IsDeleted && b.CurrentDate >= startDate && b.CurrentDate <= endDate && b.LockLevel == level)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.LockLevel, WorkLockLevel.None)
                .SetProperty(b => b.SealedAt, (DateTime?)null)
                .SetProperty(b => b.SealedBy, (string?)null), cancellationToken);
    }
}
