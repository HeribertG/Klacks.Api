// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade for work-related notifications. Bundles IWorkNotificationService,
/// IShiftStatsNotificationService, IShiftScheduleService and ScheduleMapper
/// to reduce DI overload and code duplication in the Works handlers.
/// </summary>
/// <param name="notificationService">SignalR notifications for Work/Schedule/PeriodHours events</param>
/// <param name="shiftStatsNotificationService">SignalR notifications for ShiftStats updates</param>
/// <param name="shiftScheduleService">Provides partial ShiftSchedule data for affected shift-date pairs</param>
/// <param name="scheduleMapper">Mapper for notification DTOs</param>
/// <param name="httpContextAccessor">Access to the SignalR ConnectionId header</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class WorkNotificationFacade : IWorkNotificationFacade
{
    private readonly IWorkNotificationService _notificationService;
    private readonly IShiftStatsNotificationService _shiftStatsNotificationService;
    private readonly IShiftScheduleService _shiftScheduleService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkNotificationFacade(
        IWorkNotificationService notificationService,
        IShiftStatsNotificationService shiftStatsNotificationService,
        IShiftScheduleService shiftScheduleService,
        ScheduleMapper scheduleMapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _notificationService = notificationService;
        _shiftStatsNotificationService = shiftStatsNotificationService;
        _shiftScheduleService = shiftScheduleService;
        _scheduleMapper = scheduleMapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetConnectionId()
    {
        return _httpContextAccessor.HttpContext?.Request
            .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
    }

    public async Task NotifyWorkCreatedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd)
    {
        var notification = _scheduleMapper.ToWorkNotificationDto(work, ScheduleEventTypes.Created, connectionId, periodStart, periodEnd);
        await _notificationService.NotifyWorkCreated(notification);
    }

    public async Task NotifyWorkUpdatedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd)
    {
        var notification = _scheduleMapper.ToWorkNotificationDto(work, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
        await _notificationService.NotifyWorkUpdated(notification);
    }

    public async Task NotifyWorkDeletedAsync(Work work, string connectionId, DateOnly periodStart, DateOnly periodEnd)
    {
        var notification = _scheduleMapper.ToWorkNotificationDto(work, ScheduleEventTypes.Deleted, connectionId, periodStart, periodEnd);
        await _notificationService.NotifyWorkDeleted(notification);
    }

    public async Task NotifyPeriodHoursUpdatedAsync(
        Guid clientId, DateOnly periodStart, DateOnly periodEnd,
        PeriodHoursResource periodHours, string connectionId)
    {
        var periodHoursNotification = new PeriodHoursNotificationDto
        {
            ClientId = clientId,
            StartDate = periodStart,
            EndDate = periodEnd,
            Hours = periodHours.Hours,
            Surcharges = periodHours.Surcharges,
            GuaranteedHours = periodHours.GuaranteedHours,
            SourceConnectionId = connectionId
        };
        await _notificationService.NotifyPeriodHoursUpdated(periodHoursNotification);
    }

    public async Task NotifyShiftStatsAsync(
        HashSet<(Guid ShiftId, DateOnly Date)> affectedShifts,
        string connectionId, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var shiftDatePairs = affectedShifts.ToList();
        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, analyseToken, cancellationToken);

        foreach (var shiftData in shiftStats)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }

    public async Task NotifyShiftStatsAsync(
        Guid shiftId, DateOnly date,
        string connectionId, Guid? analyseToken, CancellationToken cancellationToken)
    {
        var shiftDatePairs = new List<(Guid ShiftId, DateOnly Date)> { (shiftId, date) };
        var shiftStats = await _shiftScheduleService.GetShiftSchedulePartialAsync(shiftDatePairs, analyseToken, cancellationToken);
        var shiftData = shiftStats.FirstOrDefault();

        if (shiftData != null)
        {
            var shiftNotification = _scheduleMapper.ToShiftStatsNotificationDto(shiftData, connectionId);
            await _shiftStatsNotificationService.NotifyShiftStatsUpdated(shiftNotification);
        }
    }

    public async Task NotifyScheduleUpdatedAsync(
        Guid clientId, DateOnly currentDate,
        string connectionId, DateOnly periodStart, DateOnly periodEnd)
    {
        var notification = _scheduleMapper.ToScheduleNotificationDto(
            clientId, currentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd);
        await _notificationService.NotifyScheduleUpdated(notification);
    }
}
