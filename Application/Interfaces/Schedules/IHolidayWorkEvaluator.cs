// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports work placed on a day the client's calendar marks as a statutory holiday, unless an
/// exemption covers that client.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IHolidayWorkEvaluator
{
    /// <summary>
    /// Judges every day in [from, to] on which the client has work.
    /// </summary>
    /// <param name="clientId">The client whose calendar and contract decide holiday and exemption</param>
    /// <param name="clientName">Display name carried into the reported entries</param>
    /// <param name="workDates">Days the client actually has work on, so days off are never inspected</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        IReadOnlyCollection<DateOnly> workDates,
        CancellationToken cancellationToken = default);
}
