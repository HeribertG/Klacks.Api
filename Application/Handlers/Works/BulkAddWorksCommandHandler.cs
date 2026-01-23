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
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BulkAddWorksCommandHandler(
        IWorkRepository workRepository,
        IShiftRepository shiftRepository,
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
        _shiftRepository = shiftRepository;
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

        var shift = await _shiftRepository.Get(command.Request.ShiftId);
        if (shift == null)
        {
            _logger.LogError("Shift with ID {ShiftId} not found", command.Request.ShiftId);
            response.FailedCount = command.Request.Entries.Count;
            return response;
        }

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
                    StartTime = shift.StartShift,
                    EndTime = shift.EndShift,
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

            var affectedClients = new HashSet<Guid>();
            var clientPeriods = new Dictionary<Guid, (DateOnly Start, DateOnly End)>();

            foreach (var work in createdWorks)
            {
                affectedClients.Add(work.ClientId);

                var (start, end) = await _periodHoursService.GetPeriodBoundariesAsync(DateOnly.FromDateTime(work.CurrentDate));

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

                var notification = _scheduleMapper.ToWorkNotificationDto(work, "created", connectionId, start, end);
                await _notificationService.NotifyWorkCreated(notification);
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
                    }
                }
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
