// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates the active RestrictedTimeWindowRule set (K16) for one client: reports every planned or
/// persisted work whose shift is in a rule's group-tag scope and whose interval overlaps the rule's daily
/// forbidden window on a calendar day inside the season. Like IRestDayRotationEvaluator this is an
/// ABSOLUTE check on the projected end state. EvaluatePlannedAsync carries the ShiftId per slot (unlike
/// K10/K18) because the scope is resolved through the shift, not the client's contract.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IRestrictedTimeWindowEvaluator
{
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    // Unlike K10/K18 this evaluator is a per-work check that does NOT fan out backward from a single date,
    // so range and period-closing callers MUST pass the whole span here - a single-date call only inspects
    // that one calendar day. EvaluateAsync(date) is exactly EvaluateRangeAsync(date, date).
    Task<List<ScheduleValidationNotificationDto>> EvaluateRangeAsync(
        Guid clientId,
        string clientName,
        DateOnly from,
        DateOnly to,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, Guid? ShiftId)> plannedSlots,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
