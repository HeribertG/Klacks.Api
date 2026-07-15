// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates the active CounterRule set (K18) for one client: counts the rule's event occurrences in
/// the calendar period containing each candidate date and reports every rule whose threshold is
/// reached. Like IPeriodCapEvaluator this is an ABSOLUTE check on the projected end state (not a
/// diff): a placement in an already-over-threshold period keeps being reported.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ICounterRuleEvaluator
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
