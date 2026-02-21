using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkChangeResource?> Handle(DeleteCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting WorkChange with ID: {Id}", request.Id);

        var existingWorkChange = await _workChangeRepository.Get(request.Id);
        if (existingWorkChange == null)
        {
            _logger.LogWarning("WorkChange not found: {Id}", request.Id);
            return null;
        }

        var workId = existingWorkChange.WorkId;
        var replaceClientId = existingWorkChange.ReplaceClientId;
        var workChangeResource = _scheduleMapper.ToWorkChangeResource(existingWorkChange);

        await _workChangeRepository.Delete(request.Id);

        var work = await _workRepository.Get(workId);
        if (work == null)
        {
            _logger.LogWarning("Work not found for WorkChange: {WorkId}", workId);
            return workChangeResource;
        }

        var currentDate = work.CurrentDate;
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

        await _completionService.SaveAndTrackWithReplaceClientAsync(
            work.ClientId, work.CurrentDate, periodStart, periodEnd, replaceClientId);

        var threeDayStart = currentDate.AddDays(-1);
        var threeDayEnd = currentDate.AddDays(1);

        workChangeResource.PeriodStart = periodStart;
        workChangeResource.PeriodEnd = periodEnd;

        var clientResult = await GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
        workChangeResource.ClientResults.Add(clientResult);

        if (replaceClientId.HasValue)
        {
            var replaceClientResult = await GetClientResultAsync(replaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
            workChangeResource.ClientResults.Add(replaceClientResult);
        }

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
        var notification = _scheduleMapper.ToScheduleNotificationDto(
            work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
        await _notificationService.NotifyScheduleUpdated(notification);

        if (replaceClientId.HasValue)
        {
            var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                replaceClientId.Value, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(replaceNotification);
        }

        _logger.LogInformation("WorkChange deleted successfully: {Id}", request.Id);
        return workChangeResource;
    }

    private async Task<WorkChangeClientResult> GetClientResultAsync(
        Guid clientId,
        DateOnly periodStart,
        DateOnly periodEnd,
        DateOnly threeDayStart,
        DateOnly threeDayEnd,
        CancellationToken cancellationToken)
    {
        var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(clientId, periodStart, periodEnd);

        var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
            .Where(e => e.ClientId == clientId)
            .ToListAsync(cancellationToken);

        return new WorkChangeClientResult
        {
            ClientId = clientId,
            PeriodHours = periodHours,
            ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList()
        };
    }
}
