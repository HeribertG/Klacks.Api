// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the configurable compliance rule evaluators of a schedule timeline check for one client - period caps,
/// rest-day rotation, counter rules, restricted time windows and compensatory rest (reconcile first, then surface
/// the open obligations) - and appends their findings in that fixed order. Each evaluator resolves its own
/// severity. Scoped like the evaluators it calls.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Interfaces.Schedules;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public sealed class ScheduleTimelineComplianceRules
{
    private readonly IPeriodCapEvaluator _periodCapEvaluator;
    private readonly IRestDayRotationEvaluator _restDayRotationEvaluator;
    private readonly ICounterRuleEvaluator _counterRuleEvaluator;
    private readonly IRestrictedTimeWindowEvaluator _restrictedTimeWindowEvaluator;
    private readonly ICompensatoryRestObligationReconciler _compensatoryRestReconciler;
    private readonly ICompensatoryRestEvaluator _compensatoryRestEvaluator;

    public ScheduleTimelineComplianceRules(
        IPeriodCapEvaluator periodCapEvaluator,
        IRestDayRotationEvaluator restDayRotationEvaluator,
        ICounterRuleEvaluator counterRuleEvaluator,
        IRestrictedTimeWindowEvaluator restrictedTimeWindowEvaluator,
        ICompensatoryRestObligationReconciler compensatoryRestReconciler,
        ICompensatoryRestEvaluator compensatoryRestEvaluator)
    {
        _periodCapEvaluator = periodCapEvaluator;
        _restDayRotationEvaluator = restDayRotationEvaluator;
        _counterRuleEvaluator = counterRuleEvaluator;
        _restrictedTimeWindowEvaluator = restrictedTimeWindowEvaluator;
        _compensatoryRestReconciler = compensatoryRestReconciler;
        _compensatoryRestEvaluator = compensatoryRestEvaluator;
    }

    /// <summary>
    /// Single check: every evaluator anchors at the edited day. Compensatory-rest obligations are reconciled for
    /// the boundary window around it, then the open ones are surfaced; reconcile is real-mode only (the
    /// reconciler no-ops on a non-null token).
    /// </summary>
    public async Task AddDayEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        Guid clientId,
        string clientName,
        DateOnly date,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        entries.AddRange(await _periodCapEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
        entries.AddRange(await _restDayRotationEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
        entries.AddRange(await _counterRuleEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
        entries.AddRange(await _restrictedTimeWindowEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));

        await _compensatoryRestReconciler.ReconcileAsync(clientId, date.AddDays(-ScenarioConstants.BoundaryDays), date.AddDays(ScenarioConstants.BoundaryDays), analyseToken, cancellationToken);
        entries.AddRange(await _compensatoryRestEvaluator.EvaluateAsync(clientId, clientName, date, analyseToken, cancellationToken));
    }

    /// <summary>
    /// Range check. The anchor choice differs on purpose: the period-cap evaluator resolves calendar windows from
    /// its anchor and startDate keeps the historical behaviour; the trailing-window/counter evaluators anchor at
    /// endDate so the latest changes in the range fall inside the inspected window/period. Compensatory rest is
    /// reconciled over [startDate - boundary, endDate] so cross-boundary gaps and fulfilment are caught even when
    /// nobody re-edits the trigger date; then the open obligations are surfaced.
    /// </summary>
    public async Task AddRangeEntriesAsync(
        List<ScheduleValidationNotificationDto> entries,
        Guid clientId,
        string clientName,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        entries.AddRange(await _periodCapEvaluator.EvaluateAsync(clientId, clientName, startDate, analyseToken, cancellationToken));
        entries.AddRange(await _restDayRotationEvaluator.EvaluateAsync(clientId, clientName, endDate, analyseToken, cancellationToken));
        entries.AddRange(await _counterRuleEvaluator.EvaluateAsync(clientId, clientName, endDate, analyseToken, cancellationToken));
        entries.AddRange(await _restrictedTimeWindowEvaluator.EvaluateRangeAsync(clientId, clientName, startDate, endDate, analyseToken, cancellationToken));

        await _compensatoryRestReconciler.ReconcileAsync(clientId, startDate.AddDays(-ScenarioConstants.BoundaryDays), endDate, analyseToken, cancellationToken);
        entries.AddRange(await _compensatoryRestEvaluator.EvaluateAsync(clientId, clientName, endDate, analyseToken, cancellationToken));
    }
}
