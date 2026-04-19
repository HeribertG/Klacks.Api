// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
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
        DateOnly periodStart, DateOnly periodEnd,
        Guid? analyseToken)
    {
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        _timelineService.QueueCheck(clientId, currentDate, analyseToken);
        return await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);
    }

    public async Task<PeriodHoursResource> SaveAndTrackMoveAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? previousClientId, DateOnly? previousDate,
        Guid? analyseToken)
    {
        await _unitOfWork.CompleteAsync();
        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        _timelineService.QueueCheck(clientId, currentDate, analyseToken);

        if (previousClientId.HasValue && previousDate.HasValue &&
            (previousClientId.Value != clientId || previousDate.Value != currentDate))
        {
            await _scheduleChangeTracker.TrackChangeAsync(previousClientId.Value, previousDate.Value, analyseToken);
            _timelineService.QueueCheck(previousClientId.Value, previousDate.Value, analyseToken);
        }

        return await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);
    }

    public async Task SaveBulkAndTrackAsync(
        List<(Guid ClientId, DateOnly CurrentDate, Guid? AnalyseToken)> affectedEntries)
    {
        await _unitOfWork.CompleteAsync();

        foreach (var (clientId, currentDate, analyseToken) in affectedEntries)
        {
            await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
            _timelineService.QueueCheck(clientId, currentDate, analyseToken);
        }
    }

    public async Task SaveAndTrackWithReplaceClientAsync(
        Guid clientId, DateOnly currentDate,
        DateOnly periodStart, DateOnly periodEnd,
        Guid? replaceClientId,
        Guid? analyseToken,
        Guid? previousReplaceClientId = null)
    {
        await _unitOfWork.CompleteAsync();

        await _scheduleChangeTracker.TrackChangeAsync(clientId, currentDate, analyseToken);
        _timelineService.QueueCheck(clientId, currentDate, analyseToken);
        await RecalculateAndGetPeriodHoursAsync(clientId, periodStart, periodEnd, analyseToken);

        if (replaceClientId.HasValue)
        {
            await _scheduleChangeTracker.TrackChangeAsync(replaceClientId.Value, currentDate, analyseToken);
            _timelineService.QueueCheck(replaceClientId.Value, currentDate, analyseToken);
            await RecalculateAndGetPeriodHoursAsync(replaceClientId.Value, periodStart, periodEnd, analyseToken);
        }

        if (previousReplaceClientId.HasValue && previousReplaceClientId != replaceClientId)
        {
            await _scheduleChangeTracker.TrackChangeAsync(previousReplaceClientId.Value, currentDate, analyseToken);
            _timelineService.QueueCheck(previousReplaceClientId.Value, currentDate, analyseToken);
            await RecalculateAndGetPeriodHoursAsync(previousReplaceClientId.Value, periodStart, periodEnd, analyseToken);
        }
    }

    private async Task<PeriodHoursResource> RecalculateAndGetPeriodHoursAsync(
        Guid clientId, DateOnly periodStart, DateOnly periodEnd, Guid? analyseToken)
    {
        var connectionId = _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault();

        return await _periodHoursService.RecalculateAndNotifyAsync(
            clientId, periodStart, periodEnd, analyseToken, connectionId);
    }
}
