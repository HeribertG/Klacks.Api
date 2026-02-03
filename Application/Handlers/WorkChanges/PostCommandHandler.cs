using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PostCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkChangeResource?> Handle(PostCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new WorkChange");

        var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);
        await _workChangeRepository.Add(workChange);
        await _unitOfWork.CompleteAsync();

        var work = await _workRepository.Get(workChange.WorkId);
        if (work == null)
        {
            _logger.LogWarning("Work not found for WorkChange: {WorkId}", workChange.WorkId);
            return _scheduleMapper.ToWorkChangeResource(workChange);
        }

        var currentDate = work.CurrentDate;
        var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(currentDate);
        var threeDayStart = currentDate.AddDays(-1);
        var threeDayEnd = currentDate.AddDays(1);

        var resource = _scheduleMapper.ToWorkChangeResource(workChange);
        resource.PeriodStart = periodStart;
        resource.PeriodEnd = periodEnd;

        var clientResult = await GetClientResultAsync(work.ClientId, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
        resource.ClientResults.Add(clientResult);

        if (workChange.ReplaceClientId.HasValue)
        {
            var replaceClientResult = await GetClientResultAsync(workChange.ReplaceClientId.Value, periodStart, periodEnd, threeDayStart, threeDayEnd, cancellationToken);
            resource.ClientResults.Add(replaceClientResult);
        }

        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;
        var notification = _scheduleMapper.ToScheduleNotificationDto(
            work.ClientId, work.CurrentDate, "updated", connectionId, periodStart, periodEnd);
        await _notificationService.NotifyScheduleUpdated(notification);

        if (workChange.ReplaceClientId.HasValue)
        {
            var replaceNotification = _scheduleMapper.ToScheduleNotificationDto(
                workChange.ReplaceClientId.Value, work.CurrentDate, "updated", connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(replaceNotification);
        }

        _logger.LogInformation("WorkChange created successfully with ID: {Id}", workChange.Id);
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
        var periodHours = await _periodHoursService.RecalculateAndNotifyAsync(clientId, periodStart, periodEnd);

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
