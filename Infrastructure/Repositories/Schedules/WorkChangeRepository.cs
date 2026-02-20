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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IWorkMacroService _workMacroService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly IScheduleTimelineService _timelineService;

    public WorkChangeRepository(
        DataBaseContext context,
        ILogger<WorkChange> logger,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IWorkMacroService workMacroService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        IScheduleTimelineService timelineService)
        : base(context, logger)
    {
        _context = context;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _workMacroService = workMacroService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
        _timelineService = timelineService;
    }

    public override async Task Add(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        await base.Add(entity);
        await _unitOfWork.CompleteAsync();
        await PostSaveProcessingAsync(entity.WorkId, entity.ReplaceClientId);
    }

    public override async Task<WorkChange?> Put(WorkChange entity)
    {
        await _workMacroService.ProcessWorkChangeMacroAsync(entity);
        var result = await base.Put(entity);
        if (result == null) return null;
        await _unitOfWork.CompleteAsync();
        await PostSaveProcessingAsync(entity.WorkId, entity.ReplaceClientId);
        return result;
    }

    public override async Task<WorkChange?> Delete(Guid id)
    {
        var entity = await base.Get(id);
        var result = await base.Delete(id);
        if (entity == null) return result;
        await _unitOfWork.CompleteAsync();
        await PostSaveProcessingAsync(entity.WorkId, entity.ReplaceClientId);
        return result;
    }

    public override async Task<WorkChange?> Get(Guid id)
    {
        return await _context.WorkChange
            .AsNoTracking()
            .Include(wc => wc.Work)
            .FirstOrDefaultAsync(wc => wc.Id == id);
    }

    private async Task PostSaveProcessingAsync(Guid workId, Guid? replaceClientId)
    {
        var work = await _context.Work.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workId);
        if (work == null) return;

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

        await _periodHoursService.RecalculateAndNotifyAsync(
            work.ClientId,
            periodStart,
            periodEnd,
            connectionId);

        await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate);
        _timelineService.QueueCheck(work.ClientId, work.CurrentDate);

        if (replaceClientId.HasValue)
        {
            await _periodHoursService.RecalculateAndNotifyAsync(
                replaceClientId.Value,
                periodStart,
                periodEnd,
                connectionId);

            await _scheduleChangeTracker.TrackChangeAsync(replaceClientId.Value, work.CurrentDate);
            _timelineService.QueueCheck(replaceClientId.Value, work.CurrentDate);
        }
    }
}
