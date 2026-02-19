using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class WorkChangeRepository : BaseRepository<WorkChange>, IWorkChangeRepository
{
    private readonly DataBaseContext _context;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkMacroService _workMacroService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkChangeRepository(
        DataBaseContext context,
        ILogger<WorkChange> logger,
        IPeriodHoursService periodHoursService,
        IWorkMacroService workMacroService,
        IHttpContextAccessor httpContextAccessor)
        : base(context, logger)
    {
        _context = context;
        _periodHoursService = periodHoursService;
        _workMacroService = workMacroService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task Add(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        await base.Add(entity);
        await RecalculatePeriodHoursForWorkChangeAsync(entity.WorkId, entity.ReplaceClientId);
    }

    public override async Task<WorkChange?> Put(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        var result = await base.Put(entity);
        await RecalculatePeriodHoursForWorkChangeAsync(entity.WorkId, entity.ReplaceClientId);
        return result;
    }

    public override async Task<WorkChange?> Delete(Guid id)
    {
        var entity = await base.Get(id);
        var result = await base.Delete(id);
        if (entity != null)
        {
            await RecalculatePeriodHoursForWorkChangeAsync(entity.WorkId, entity.ReplaceClientId);
        }
        return result;
    }

    public override async Task<WorkChange?> Get(Guid id)
    {
        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .FirstOrDefaultAsync(wc => wc.Id == id);
    }

    private async Task RecalculatePeriodHoursForWorkChangeAsync(Guid workId, Guid? replaceClientId)
    {
        var work = await _context.Work.FirstOrDefaultAsync(w => w.Id == workId);
        if (work == null) return;

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

        await _periodHoursService.RecalculateAndNotifyAsync(
            work.ClientId,
            periodStart,
            periodEnd,
            connectionId);

        if (replaceClientId.HasValue)
        {
            await _periodHoursService.RecalculateAndNotifyAsync(
                replaceClientId.Value,
                periodStart,
                periodEnd,
                connectionId);
        }
    }
}
