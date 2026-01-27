using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Hubs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteWorkCommand, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly IWorkScheduleService _workScheduleService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        IWorkScheduleService workScheduleService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _workScheduleService = workScheduleService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkResource?> Handle(DeleteWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.Id);
            if (work == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }

            var shiftId = work.ShiftId;
            var workDate = work.CurrentDate;

            var (deletedWork, periodHours) = await _workRepository.DeleteWithPeriodHours(request.Id, request.PeriodStart, request.PeriodEnd);
            await _unitOfWork.CompleteAsync();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;

            var notification = _scheduleMapper.ToWorkNotificationDto(work, "deleted", connectionId, request.PeriodStart, request.PeriodEnd);
            await _notificationService.NotifyWorkDeleted(notification);

            await SendShiftStatsNotificationAsync(shiftId, workDate, connectionId, cancellationToken);

            var currentDate = DateOnly.FromDateTime(work.CurrentDate);
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var scheduleEntries = await _workScheduleService.GetWorkScheduleQuery(threeDayStart, threeDayEnd)
                .Where(e => e.ClientId == work.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        },
        "deleting work",
        new { WorkId = request.Id });
    }

    private async Task SendShiftStatsNotificationAsync(
        Guid shiftId,
        DateTime date,
        string connectionId,
        CancellationToken cancellationToken)
    {
        var shiftDatePairs = new List<(Guid ShiftId, DateOnly Date)>
        {
            (shiftId, DateOnly.FromDateTime(date))
        };

        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, cancellationToken);
        var shiftData = shiftStats.FirstOrDefault();

        if (shiftData != null)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }
}
