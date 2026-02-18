using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkDeleteWorksCommandHandler : BaseHandler, IRequestHandler<BulkDeleteWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BulkDeleteWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
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
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BulkWorksResponse> Handle(BulkDeleteWorksCommand command, CancellationToken cancellationToken)
    {
        var response = new BulkWorksResponse();
        var deletedWorks = new List<Work>();
        var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>();
        var affectedClients = new HashSet<Guid>();

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

                affectedShifts.Add((work.ShiftId, work.CurrentDate));
                affectedClients.Add(work.ClientId);
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

            var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();

            foreach (var work in deletedWorks)
            {
                var (start, end) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

                if (!clientPeriods.ContainsKey(work.ClientId))
                {
                    clientPeriods[work.ClientId] = (start, end);
                }
                else
                {
                    var existing = clientPeriods[work.ClientId];
                    clientPeriods[work.ClientId] = (
                        start < existing.Start ? start : existing.Start,
                        end > existing.End ? end : existing.End
                    );
                }

                var notification = _scheduleMapper.ToWorkNotificationDto(work, "deleted", connectionId, start, end);
                await _notificationService.NotifyWorkDeleted(notification);
            }

            if (affectedClients.Count > 0)
            {
                response.PeriodHours = new Dictionary<Guid, PeriodHoursResource>();

                foreach (var clientId in affectedClients)
                {
                    if (clientPeriods.TryGetValue(clientId, out var period))
                    {
                        var periodHours = await _periodHoursService.CalculatePeriodHoursAsync(
                            clientId,
                            period.Start,
                            period.End);
                        response.PeriodHours[clientId] = periodHours;

                        var periodHoursNotification = new Application.DTOs.Notifications.PeriodHoursNotificationDto
                        {
                            ClientId = clientId,
                            StartDate = period.Start,
                            EndDate = period.End,
                            Hours = periodHours.Hours,
                            Surcharges = periodHours.Surcharges,
                            GuaranteedHours = periodHours.GuaranteedHours,
                            SourceConnectionId = connectionId
                        };
                        await _notificationService.NotifyPeriodHoursUpdated(periodHoursNotification);
                    }
                }
            }

            await SendShiftStatsNotificationsAsync(affectedShifts, connectionId, cancellationToken);
        }

        response.AffectedShifts = affectedShifts
            .Select(x => new ShiftDatePair { ShiftId = x.ShiftId, Date = x.Date.ToDateTime(TimeOnly.MinValue) })
            .ToList();

        return response;
    }

    private async Task SendShiftStatsNotificationsAsync(
        HashSet<(Guid ShiftId, DateOnly Date)> affectedShifts,
        string connectionId,
        CancellationToken cancellationToken)
    {
        var shiftDatePairs = affectedShifts.ToList();

        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, cancellationToken);

        foreach (var shiftData in shiftStats)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }
}
