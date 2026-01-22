using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class WorkChangeRepository : BaseRepository<WorkChange>, IWorkChangeRepository
{
    private readonly DataBaseContext _context;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkChangeRepository(
        DataBaseContext context,
        ILogger<WorkChange> logger,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor)
        : base(context, logger)
    {
        _context = context;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task Add(WorkChange entity)
    {
        await base.Add(entity);
        await RecalculatePeriodHoursForWorkChangeAsync(entity.WorkId);
    }

    public override async Task<WorkChange?> Put(WorkChange entity)
    {
        var result = await base.Put(entity);
        await RecalculatePeriodHoursForWorkChangeAsync(entity.WorkId);
        return result;
    }

    public override async Task<WorkChange?> Delete(Guid id)
    {
        var entity = await base.Get(id);
        var result = await base.Delete(id);
        if (entity != null)
        {
            await RecalculatePeriodHoursForWorkChangeAsync(entity.WorkId);
        }
        return result;
    }

    private async Task RecalculatePeriodHoursForWorkChangeAsync(Guid workId)
    {
        var work = await _context.Work.FirstOrDefaultAsync(w => w.Id == workId);
        if (work != null)
        {
            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers["X-SignalR-ConnectionId"].FirstOrDefault();

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(work.CurrentDate));

            await _periodHoursService.RecalculateAndNotifyAsync(
                work.ClientId,
                periodStart,
                periodEnd,
                connectionId);
        }
    }
}
