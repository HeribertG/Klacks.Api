using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Breaks;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteBreakCommand, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DeleteCommandHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<BreakResource?> Handle(DeleteBreakCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntry = await _breakRepository.Get(request.Id);
            if (breakEntry == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found.");
            }

            await _breakRepository.Delete(request.Id);
            var periodHours = await _completionService.SaveAndTrackAsync(
                breakEntry.ClientId, breakEntry.CurrentDate, request.PeriodStart, request.PeriodEnd);

            var currentDate = breakEntry.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesQuery(threeDayStart, threeDayEnd)
                .Where(e => e.ClientId == breakEntry.ClientId)
                .ToListAsync(cancellationToken);

            var breakResource = _scheduleMapper.ToBreakResource(breakEntry);
            breakResource.PeriodHours = periodHours;
            breakResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                breakEntry.ClientId, breakEntry.CurrentDate, ScheduleEventTypes.Updated, connectionId, request.PeriodStart, request.PeriodEnd);
            await _notificationService.NotifyScheduleUpdated(notification);

            return breakResource;
        },
        "deleting break",
        new { BreakId = request.Id });
    }
}
