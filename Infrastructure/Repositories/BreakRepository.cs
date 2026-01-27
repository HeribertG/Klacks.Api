using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Infrastructure.Repositories;

public class BreakRepository : BaseRepository<Break>, IBreakRepository
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IBreakMacroService _breakMacroService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BreakRepository(
        DataBaseContext context,
        ILogger<Break> logger,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IBreakMacroService breakMacroService,
        IHttpContextAccessor httpContextAccessor)
        : base(context, logger)
    {
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _breakMacroService = breakMacroService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task Add(Break entity)
    {
        await _breakMacroService.ProcessBreakMacroAsync(entity);
        await base.Add(entity);
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(entity.CurrentDate));
        await RecalculatePeriodHoursAsync(entity.ClientId, periodStart, periodEnd);
    }

    public override async Task<Break?> Put(Break entity)
    {
        await _breakMacroService.ProcessBreakMacroAsync(entity);
        var result = await base.Put(entity);
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(entity.CurrentDate));
        await RecalculatePeriodHoursAsync(entity.ClientId, periodStart, periodEnd);
        return result;
    }

    public override async Task<Break?> Delete(Guid id)
    {
        var entity = await base.Get(id);
        var result = await base.Delete(id);
        if (entity != null)
        {
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(entity.CurrentDate));
            await RecalculatePeriodHoursAsync(entity.ClientId, periodStart, periodEnd);
        }
        return result;
    }

    public async Task<(Break Break, PeriodHoursResource PeriodHours)> AddWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd)
    {
        await _breakMacroService.ProcessBreakMacroAsync(breakEntry);
        await base.Add(breakEntry);
        await _unitOfWork.CompleteAsync();
        var periodHours = await RecalculateAndGetPeriodHoursAsync(breakEntry.ClientId, periodStart, periodEnd);
        return (breakEntry, periodHours);
    }

    public async Task<(Break? Break, PeriodHoursResource? PeriodHours)> PutWithPeriodHours(Break breakEntry, DateOnly periodStart, DateOnly periodEnd)
    {
        await _breakMacroService.ProcessBreakMacroAsync(breakEntry);
        var result = await base.Put(breakEntry);
        if (result == null) return (null, null);
        await _unitOfWork.CompleteAsync();
        var periodHours = await RecalculateAndGetPeriodHoursAsync(breakEntry.ClientId, periodStart, periodEnd);
        return (result, periodHours);
    }

    public async Task<(Break? Break, PeriodHoursResource? PeriodHours)> DeleteWithPeriodHours(Guid id, DateOnly periodStart, DateOnly periodEnd)
    {
        var breakEntry = await base.Get(id);
        var result = await base.Delete(id);
        if (breakEntry == null) return (null, null);
        await _unitOfWork.CompleteAsync();
        var periodHours = await RecalculateAndGetPeriodHoursAsync(breakEntry.ClientId, periodStart, periodEnd);
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
}
