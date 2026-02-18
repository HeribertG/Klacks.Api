using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;

    public PutCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = _scheduleMapper.ToBreakEntity(request.Resource);

            DateOnly periodStart;
            DateOnly periodEnd;

            if (request.Resource.PeriodStart.HasValue && request.Resource.PeriodEnd.HasValue)
            {
                periodStart = request.Resource.PeriodStart.Value;
                periodEnd = request.Resource.PeriodEnd.Value;
            }
            else
            {
                (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(entity.CurrentDate);
            }

            var (updated, periodHours) = await _breakRepository.PutWithPeriodHours(entity, periodStart, periodEnd);

            if (updated == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found");
            }

            await _unitOfWork.CompleteAsync();
            await _scheduleChangeTracker.TrackChangeAsync(updated.ClientId, updated.CurrentDate);

            var currentDate = updated.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
                .Where(e => e.ClientId == updated.ClientId)
                .ToListAsync(cancellationToken);

            var breakResource = _scheduleMapper.ToBreakResource(updated);
            breakResource.PeriodHours = periodHours;
            breakResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers["X-SignalR-ConnectionId"].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                updated.ClientId, updated.CurrentDate, "updated", connectionId, periodStart, periodEnd);
            await _notificationService.NotifyScheduleUpdated(notification);

            return breakResource;
        }, "UpdateBreak", new { request.Resource.Id });
    }
}
