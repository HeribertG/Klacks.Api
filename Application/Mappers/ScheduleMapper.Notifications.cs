// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partial class for notification DTOs (work, shift stats, schedule notifications).
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Mappers;

public partial class ScheduleMapper
{
    public WorkNotificationDto ToWorkNotificationDto(
        Work work,
        string operationType,
        string sourceConnectionId,
        DateOnly periodStartDate,
        DateOnly periodEndDate)
    {
        return new WorkNotificationDto
        {
            WorkId = work.Id,
            ClientId = work.ClientId,
            ShiftId = work.ShiftId,
            CurrentDate = work.CurrentDate,
            PeriodStartDate = periodStartDate,
            PeriodEndDate = periodEndDate,
            OperationType = operationType,
            SourceConnectionId = sourceConnectionId
        };
    }

    public ShiftStatsNotificationDto ToShiftStatsNotificationDto(ShiftDayAssignment shiftData, string sourceConnectionId)
    {
        return new ShiftStatsNotificationDto
        {
            ShiftId = shiftData.ShiftId,
            Date = shiftData.Date.ToDateTime(TimeOnly.MinValue),
            Engaged = shiftData.Engaged,
            SourceConnectionId = sourceConnectionId
        };
    }

    public ScheduleNotificationDto ToScheduleNotificationDto(
        Guid clientId,
        DateOnly currentDate,
        string operationType,
        string sourceConnectionId,
        DateOnly periodStartDate,
        DateOnly periodEndDate)
    {
        return new ScheduleNotificationDto
        {
            ClientId = clientId,
            CurrentDate = currentDate,
            PeriodStartDate = periodStartDate,
            PeriodEndDate = periodEndDate,
            OperationType = operationType,
            SourceConnectionId = sourceConnectionId
        };
    }
}
