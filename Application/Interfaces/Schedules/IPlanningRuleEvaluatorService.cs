// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Post-hoc and pre-commit evaluation of the approved PlanningConstraint family (sequence rules and team
/// fairness) against persisted Work rows, reported as schedule validation entries with the generic
/// planning-rule key. CounterRule is deliberately not part of it - CounterRuleEvaluator reports those.
/// Hard findings are Error entries tagged with the planningRule enforcement name, soft findings Warnings.
/// </summary>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IPlanningRuleEvaluatorService
{
    /// <summary>
    /// All findings inside [from, until] for the given clients, team fairness included (a team finding has
    /// ClientId Guid.Empty). Rows outside the range are only read as neighbour days.
    /// </summary>
    /// <param name="clientIds">Clients whose plan is checked; also the set team fairness compares</param>
    /// <param name="from">First day of the checked range (inclusive)</param>
    /// <param name="until">Last day of the checked range (inclusive)</param>
    /// <param name="analyseToken">Scenario token; null for the real plan</param>
    /// <param name="clientNames">Display names for the entries; missing ids get an empty name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<ScheduleValidationNotificationDto>> EvaluateRangeAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        IReadOnlyDictionary<Guid, string> clientNames,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sequence-rule findings of one client dated exactly on <paramref name="date"/>; the neighbourhood the
    /// rules look at is read around it. Team fairness is left out, it is not a property of one client-day.
    /// </summary>
    Task<List<ScheduleValidationNotificationDto>> EvaluateDayAsync(
        Guid clientId,
        string clientName,
        DateOnly date,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sequence-rule findings the planned write would newly create or worsen: the persisted plan is evaluated
    /// before and after applying <paramref name="plannedRows"/> and <paramref name="removals"/>, and only a
    /// (rule, client) whose total excess rises is reported. Pre-existing violations never block an unrelated
    /// write. Team fairness is left out, a write only sees the clients it touches.
    /// </summary>
    Task<List<ScheduleValidationNotificationDto>> EvaluatePlannedChangeAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        IReadOnlyList<PlannedRemovalRow> removals,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);
}
