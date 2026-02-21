using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PutCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PutCommandHandler> logger)
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

    public async Task<WorkChangeResource?> Handle(PutCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating WorkChange with ID: {Id}", request.Resource.Id);

        var existingWorkChange = await _workChangeRepository.Get(request.Resource.Id);
        if (existingWorkChange == null)
        {
            _logger.LogWarning("WorkChange not found: {Id}", request.Resource.Id);
            return null;
        }

        var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);
        var updatedWorkChange = await _workChangeRepository.Put(workChange);

        if (updatedWorkChange == null)
        {
            return null;
        }

        var work = await _workRepository.Get(updatedWorkChange.WorkId);
        if (work == null)
        {
            _logger.LogWarning("Work not found for WorkChange: {WorkId}", updatedWorkChange.WorkId);
            return _scheduleMapper.ToWorkChangeResource(updatedWorkChange);
        }

        var currentDate = work.CurrentDate;
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);

        await _completionService.SaveAndTrackWithReplaceClientAsync(
            work.ClientId, work.CurrentDate, periodStart, periodEnd, updatedWorkChange.ReplaceClientId);

        var threeDayStart = currentDate.AddDays(-1);
        var threeDayEnd = currentDate.AddDays(1);

        var resource = _scheduleMapper.ToWorkChangeResource(updatedWorkChange);
        resource.PeriodStart = periodStart;
        resource.PeriodEnd = periodEnd;

        var clientResult = await GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
        resource.ClientResults.Add(clientResult);

        if (updatedWorkChange.ReplaceClientId.HasValue)
        {
            var replaceClientResult = await GetClientResultAsync(updatedWorkChange.ReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
            resource.ClientResults.Add(replaceClientResult);
        }

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
        var notification = _scheduleMapper.ToScheduleNotificationDto(
            work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
        await _notificationService.NotifyScheduleUpdated(notification);

        if (updatedWorkChange.ReplaceClientId.HasValue)
        {
            var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                updatedWorkChange.ReplaceClientId.Value, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(replaceNotification);
        }

        _logger.LogInformation("WorkChange updated successfully: {Id}", request.Resource.Id);
        return resource;
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
