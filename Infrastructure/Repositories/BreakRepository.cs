using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories;

public class BreakRepository : BaseRepository<Break>, IBreakRepository
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BreakRepository(
        DataBaseContext context,
        ILogger<Break> logger,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor)
        : base(context, logger)
    {
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task Add(Break entity)
    {
        await base.Add(entity);
        await RecalculatePeriodHoursAsync(entity.ClientId, entity.CurrentDate);
    }

    public override async Task<Break?> Put(Break entity)
    {
        var result = await base.Put(entity);
        await RecalculatePeriodHoursAsync(entity.ClientId, entity.CurrentDate);
        return result;
    }

    public override async Task<Break?> Delete(Guid id)
    {
        var entity = await base.Get(id);
        var result = await base.Delete(id);
        if (entity != null)
        {
            await RecalculatePeriodHoursAsync(entity.ClientId, entity.CurrentDate);
        }
        return result;
    }

    private async Task RecalculatePeriodHoursAsync(Guid clientId, DateTime currentDate)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault();

        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(currentDate));

        await _periodHoursService.RecalculateAndNotifyAsync(
            clientId,
            periodStart,
            periodEnd,
            connectionId);
    }
}
