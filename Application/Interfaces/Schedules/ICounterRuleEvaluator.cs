// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Evaluates the active CounterRule set (K18) for one client: counts the rule's event occurrences in
/// the calendar period containing each candidate date and reports every rule whose threshold is
/// reached. Like IPeriodCapEvaluator this is an ABSOLUTE check on the projected end state (not a
/// diff): a placement in an already-over-threshold period keeps being reported.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;

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

    /// <summary>
    /// The planned evaluation for the rows of several clients in one call: the rows are grouped by client, and per
    /// client the entries of EvaluatePlannedAsync over its (date, start, end) slots are reported with an empty client
    /// name, in the order the clients first appear. The rule list, the enforcement mode,
    /// the contract data and the Work rows of the counted periods are read for all clients together.
    /// </summary>
    /// <param name="plannedRows">Not-yet-saved rows of any number of clients</param>
    /// <param name="analyseToken">Scenario token; null evaluates the real (non-scenario) schedule</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedRowsAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
