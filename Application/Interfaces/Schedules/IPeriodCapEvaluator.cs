// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates a client's hours against every active PeriodCapRule, in whichever of the two mutually
/// exclusive modes each rule uses: fixed-period cumulative totals (K5 stage 1: TotalHours scope only) or
/// K6 rolling-average weekly hours over a trailing window. Reports every rule the projection exceeds.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IPeriodCapEvaluator
{
    /// <summary>
    /// Evaluates already-persisted state: for each active rule, sums the client's persisted period hours
    /// for the window containing asOfDate and reports every rule the total exceeds.
    /// </summary>
    /// <param name="clientId">Employee whose period hours are evaluated</param>
    /// <param name="clientName">Display name for the resulting notification entries</param>
    /// <param name="asOfDate">Day the enclosing period (month/quarter/year) is resolved from</param>
    /// <param name="analyseToken">Scenario token; null evaluates the real (non-scenario) schedule</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluateAsync(
        Guid clientId,
        string clientName,
        DateOnly asOfDate,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a not-yet-persisted placement batch on top of persisted state: for each active rule,
    /// planned entries are grouped by the period window their date falls into, the persisted baseline for
    /// that window is loaded, the group's hours are added on top, and every rule/window combination whose
    /// projected total exceeds the cap is reported (report date = earliest planned date in that window).
    /// </summary>
    /// <param name="clientId">Employee the planned placements belong to</param>
    /// <param name="clientName">Display name for the resulting notification entries</param>
    /// <param name="plannedHours">Not-yet-saved (date, hours) pairs, e.g. one per planned shift</param>
    /// <param name="analyseToken">Scenario token; null evaluates the real (non-scenario) schedule</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedAsync(
        Guid clientId,
        string clientName,
        IReadOnlyList<(DateOnly Date, decimal Hours)> plannedHours,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
