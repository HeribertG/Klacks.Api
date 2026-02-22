// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Services.Schedules;

public class ScheduleCompletionService : IScheduleCompletionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly IScheduleTimelineService _timelineService;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ScheduleCompletionService(
        IUnitOfWork unitOfWork,
        IScheduleChangeTracker scheduleChangeTracker,
        IScheduleTimelineService timelineService,
        IPeriodHoursService periodHoursService,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _scheduleChangeTracker = scheduleChangeTracker;
        _timelineService = timelineService;
        _periodHoursService = periodHoursService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<PeriodHoursResource> SaveAndTrackAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd)
    {
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate);
        _timelineService.QueueCheck(clientId, currentDate);
        return await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd);
    }

    public async Task<PeriodHoursResource> SaveAndTrackMoveAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? previousClientId, DateOnly? previousDate)
    {
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate);
        _timelineService.QueueCheck(clientId, currentDate);

        if (previousClientId.HasValue && previousDate.HasValue &&
            (previousClientId.Value != clientId || previousDate.Value != currentDate))
        {
            await _scheduleChangeTracker.TrackChangeAsync(previousClientId.Value, previousDate.Value);
            _timelineService.QueueCheck(previousClientId.Value, previousDate.Value);
        }

        return await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd);
    }

    public async Task SaveBulkAndTrackAsync(
        List<(Guid ClientId, DateOnly CurrentDate)> affectedEntries)
    {
        await _unitOfWork.CompleteAsync();

        foreach (var (clientId, currentDate) in affectedEntries)
        {
            await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate);
            _timelineService.QueueCheck(clientId, currentDate);
        }
    }

    public async Task SaveAndTrackWithReplaceClientAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? replaceClientId)
    {
        await _unitOfWork.CompleteAsync();

        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate);
        _timelineService.QueueCheck(clientId, currentDate);
        await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd);

        if (replaceClientId.HasValue)
        {
            await _scheduleChangeTracker.TrackChangeAsync(replaceClientId.Value, currentDate);
            _timelineService.QueueCheck(replaceClientId.Value, currentDate);
            await RecalculateAndGetPeriodHoursAsync(replaceClientId.Value, periodStart, periodEnd);
        }
    }

    private async Task<PeriodHoursResource> RecalculateAndGetPeriodHoursAsync(
        Guid clientId, DateOnly periodStart, DateOnly periodEnd)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        return await _periodHoursService.RecalculateAndNotifyAsync(
            clientId, periodStart, periodEnd, connectionId);
    }
}
