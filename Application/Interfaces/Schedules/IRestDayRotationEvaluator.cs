// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates the active RestDayRotationRule set (K10) for one client: counts work-free occurrences of
/// the rule's weekday in the trailing window and reports every rule whose minimum is violated. Like
/// IPeriodCapEvaluator this is an ABSOLUTE check on the projected end state (not a diff): a placement
/// on an already-violating window keeps being reported until the window recovers.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IRestDayRotationEvaluator
{
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime)> plannedSlots,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
