// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IBreakMacroService _breakMacroService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;

    public PutCommandHandler(
        IBreakRepository breakRepository,
        IBreakMacroService breakMacroService,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _breakMacroService = breakMacroService;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _breakRepository.GetNoTracking(request.Resource.Id);
            var entity = _scheduleMapper.ToBreakEntity(request.Resource);

            if (existing != null)
            {
                await _dayLockService.EnsureNotLockedAsync(
                    existing.CurrentDate,
                    existing.ClientId,
                    entity.AnalyseToken,
                    cancellationToken);
            }

            await _dayLockService.EnsureNotLockedAsync(
                entity.CurrentDate,
                entity.ClientId,
                entity.AnalyseToken,
                cancellationToken);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(entity.CurrentDate);

            await _breakMacroService.ProcessBreakMacroAsync(entity, request.Resource.PaymentInterval);
            var updated = await _breakRepository.Put(entity);

            if (updated == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found");
            }

            var periodHours = await _completionService.SaveAndTrackAsync(
                updated.ClientId, updated.CurrentDate, periodStart, periodEnd, updated.AnalyseToken);

            var currentDate = updated.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService
                .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, updated.AnalyseToken)
                .Where(e => e.ClientId == updated.ClientId)
                .ToListAsync(cancellationToken);

            var breakResource = _scheduleMapper.ToBreakResource(updated);
            breakResource.PeriodHours = periodHours;
            breakResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                updated.ClientId, updated.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, updated.AnalyseToken);
            await _notificationService.NotifyScheduleUpdated(notification);

            return breakResource;
        }, "UpdateBreak", new { request.Resource.Id });
    }
}
