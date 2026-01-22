using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class BulkAddWorksCommandHandler : BaseHandler, IRequestHandler<BulkAddWorksCommand, BulkWorksResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<BulkAddWorksCommandHandler> logger)
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

    public async Task<BulkWorksResponse> Handle(BulkAddWorksCommand command, CancellationToken cancellationToken)
    {
        var response = new BulkWorksResponse();
        var createdWorks = new List<Work>();
        var affectedShifts = new HashSet<(Guid ShiftId, DateTime Date)>();

        foreach (var entry in command.Request.Entries)
        {
            try
            {
                var work = new Work
                {
                    Id = Guid.NewGuid(),
                    ShiftId = command.Request.ShiftId,
                    ClientId = entry.ClientId,
                    CurrentDate = entry.CurrentDate,
                    WorkTime = command.Request.WorkTime,
                    IsSealed = false,
                    IsDeleted = false
                };

                await _workRepository.Add(work);
                createdWorks.Add(work);
                response.CreatedIds.Add(work.Id);
                response.SuccessCount++;

                affectedShifts.Add((command.Request.ShiftId, entry.CurrentDate.Date));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create work for client {ClientId} on {Date}",
                    entry.ClientId, entry.CurrentDate);
                response.FailedCount++;
            }
        }

        if (createdWorks.Count > 0)
        {
            await _unitOfWork.CompleteAsync();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;

            foreach (var work in createdWorks)
            {
                var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(work.CurrentDate));
                var notification = _scheduleMapper.ToWorkNotificationDto(work, "created", connectionId, periodStart, periodEnd);
                await _notificationService.NotifyWorkCreated(notification);
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
