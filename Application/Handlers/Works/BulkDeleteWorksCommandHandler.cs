using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkDeleteWorksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BulkDeleteWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BulkDeleteWorksCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BulkWorksResponse> Handle(BulkDeleteWorksCommand command, CancellationToken cancellationToken)
    {
        var response = new BulkWorksResponse();
        var deletedWorks = new List<Work>();
        var affectedShifts = new HashSet<(Guid ShiftId, DateTime Date)>();

        foreach (var workId in command.Request.WorkIds)
        {
            try
            {
                var work = await _workRepository.Get(workId);
                if (work == null)
                {
                    _logger.LogWarning("Work with ID {WorkId} not found for deletion", workId);
                    response.FailedCount++;
                    continue;
                }

                affectedShifts.Add((work.ShiftId, work.CurrentDate.Date));
                deletedWorks.Add(work);

                await _workRepository.Delete(workId);
                response.DeletedIds.Add(workId);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete work {WorkId}", workId);
                response.FailedCount++;
            }
        }

        if (deletedWorks.Count > 0)
        {
            await _unitOfWork.CompleteAsync();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;

            foreach (var work in deletedWorks)
            {
                var notification = _scheduleMapper.ToWorkNotificationDto(work, "deleted", connectionId);
                await _notificationService.NotifyWorkDeleted(notification);
            }

            await SendShiftStatsNotificationsAsync(affectedShifts, connectionId, cancellationToken);
        }

        response.AffectedShifts = affectedShifts
            .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date })
            .ToList();

        return response;
    }

    private async Task SendShiftStatsNotificationsAsync(
        HashSet<(Guid ShiftId, DateTime Date)> affectedShifts,
        string connectionId,
        CancellationToken cancellationToken)
    {
        var shiftDatePairs = affectedShifts
            .Select(x => (x.ShiftId, DateOnly.FromDateTime(x.Date)))
            .ToList();

        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, cancellationToken);

        foreach (var shiftData in shiftStats)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }
}
