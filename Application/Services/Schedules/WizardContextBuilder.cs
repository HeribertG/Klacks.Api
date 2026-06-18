// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardContextBuilder"/>.
/// Composes the agent snapshot, shift expansion and hard-constraint sub-builders into a full
/// <see cref="CoreWizardContext"/> for the GA to consume. The scheduling-policy fallbacks
/// (<see cref="CoreWizardContext.SchedulingMinPauseHours"/> etc.) are loaded from the global
/// settings table so agents without contract overrides still get the system-wide defaults.
/// </summary>
/// <param name="agentBuilder">Builder that produces CoreAgent and CoreContractDay snapshots</param>
/// <param name="shiftBuilder">Builder that expands shift definitions to per-day slots</param>
/// <param name="hardConstraintBuilder">Builder that loads commands, preferences, breaks and locked works</param>
/// <param name="periodHoursService">Provider for the hours already worked in the active period</param>
/// <param name="contractProvider">Source of effective contract data; supplies system-wide defaults via empty client lookup</param>
public sealed class WizardContextBuilder : IWizardContextBuilder
{
    private readonly WizardAgentSnapshotBuilder _agentBuilder;
    private readonly IWizardShiftBuilder _shiftBuilder;
    private readonly IWizardHardConstraintBuilder _hardConstraintBuilder;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IClientContractDataProvider _contractProvider;
    private readonly IEligibilityMatrixBuilder _eligibilityMatrixBuilder;

    public WizardContextBuilder(
        WizardAgentSnapshotBuilder agentBuilder,
        IWizardShiftBuilder shiftBuilder,
        IWizardHardConstraintBuilder hardConstraintBuilder,
        IPeriodHoursService periodHoursService,
        IClientContractDataProvider contractProvider,
        IEligibilityMatrixBuilder eligibilityMatrixBuilder)
    {
        _agentBuilder = agentBuilder;
        _shiftBuilder = shiftBuilder;
        _hardConstraintBuilder = hardConstraintBuilder;
        _periodHoursService = periodHoursService;
        _contractProvider = contractProvider;
        _eligibilityMatrixBuilder = eligibilityMatrixBuilder;
    }

    public async Task<CoreWizardContext> BuildContextAsync(WizardContextRequest request, CancellationToken ct)
    {
        var currentHours = await LoadCurrentHoursAsync(request, ct);

        var agentSnapshot = await _agentBuilder.BuildAsync(
            request.AgentIds, request.PeriodFrom, request.PeriodUntil, currentHours, ct);

        var shifts = await _shiftBuilder.BuildAsync(
            request.ShiftIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);

        // Precompute which (agent, shift, date) assignments are blocked by missing mandatory
        // qualifications so the optimizer only needs an O(1) lookup during the GA.
        var eligibilityMatrix = await _eligibilityMatrixBuilder.BuildAsync(
            request.AgentIds, EligibilityMatrixBuilder.SlotsFromShifts(shifts), ct);

        // ScheduleCommands and ShiftPreferences are intentionally restricted to the planning period —
        // user-entered free/preference instructions outside the period are not relevant for this run.
        var periodConstraints = await _hardConstraintBuilder.BuildAsync(
            request.AgentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);

        // Boundary context: load the same constraint set for the wider window, then keep only the entries
        // strictly outside [PeriodFrom, PeriodUntil]. Engine validators can use these to detect MaxConsecutive /
        // MinRest runs that cross period edges; the GA never plans, scores or mutates them.
        var contextDaysBefore = Math.Max(0, request.ContextDaysBefore);
        var contextDaysAfter = Math.Max(0, request.ContextDaysAfter);
        var contextFrom = request.PeriodFrom.AddDays(-contextDaysBefore);
        var contextUntil = request.PeriodUntil.AddDays(contextDaysAfter);

        IReadOnlyList<CoreBreakBlocker> boundaryBreaks = [];
        IReadOnlyList<CoreLockedWork> boundaryLocked = [];
        IReadOnlyList<CoreExistingWorkBlocker> boundaryExisting = [];
        if (contextFrom < request.PeriodFrom || contextUntil > request.PeriodUntil)
        {
            var widerConstraints = await _hardConstraintBuilder.BuildAsync(
                request.AgentIds, contextFrom, contextUntil, request.AnalyseToken, ct);

            boundaryBreaks = widerConstraints.BreakBlockers
                .Where(b => b.FromInclusive < request.PeriodFrom || b.UntilInclusive > request.PeriodUntil)
                .ToList();
            boundaryLocked = widerConstraints.LockedWorks
                .Where(w => w.Date < request.PeriodFrom || w.Date > request.PeriodUntil)
                .ToList();
            boundaryExisting = widerConstraints.ExistingWorkBlockers
                .Where(w => w.Date < request.PeriodFrom || w.Date > request.PeriodUntil)
                .ToList();
        }

        var defaults = await _contractProvider.GetEffectiveContractDataAsync(Guid.Empty, request.PeriodFrom);

        return new CoreWizardContext
        {
            PeriodFrom = request.PeriodFrom,
            PeriodUntil = request.PeriodUntil,
            Agents = BuildRosterPriorityOrder(agentSnapshot.Agents, request.AgentOrderIsUserDefined),
            Shifts = shifts,
            ContractDays = agentSnapshot.ContractDays,
            ScheduleCommands = periodConstraints.ScheduleCommands,
            ShiftPreferences = periodConstraints.ShiftPreferences,
            BreakBlockers = periodConstraints.BreakBlockers,
            LockedWorks = periodConstraints.LockedWorks,
            ExistingWorkBlockers = periodConstraints.ExistingWorkBlockers,
            BoundaryBreakBlockers = boundaryBreaks,
            BoundaryLockedWorks = boundaryLocked,
            BoundaryExistingWorkBlockers = boundaryExisting,
            IneligibleAssignments = eligibilityMatrix.Ineligible,
            SchedulingMaxConsecutiveDays = defaults.MaxConsecutiveDays > 0 ? defaults.MaxConsecutiveDays : WizardSchedulingDefaults.MaxConsecutiveDays,
            SchedulingMinPauseHours = defaults.MinPauseHours > 0 ? (double)defaults.MinPauseHours : WizardSchedulingDefaults.MinRestHours,
            SchedulingMaxOptimalGap = defaults.MaxOptimalGap > 0 ? (double)defaults.MaxOptimalGap : 2,
            SchedulingMaxDailyHours = defaults.MaxDailyHours > 0 ? (double)defaults.MaxDailyHours : WizardSchedulingDefaults.MaxDailyHours,
            SchedulingMaxWeeklyHours = defaults.MaxWeeklyHours > 0 ? (double)defaults.MaxWeeklyHours : WizardSchedulingDefaults.MaxWeeklyHours,
            AnalyseToken = request.AnalyseToken,
        };
    }

    /// <summary>
    /// Establishes the canonical top-down roster priority order consumed by the optimizer:
    /// position 0 gets the most accurate plan, the bottom takes what is left. A hand-arranged
    /// user order is kept verbatim; the regular list order is reshaped by contractually
    /// guaranteed hours (descending). The sort is stable, so ties keep the incoming order.
    /// </summary>
    private static IReadOnlyList<CoreAgent> BuildRosterPriorityOrder(
        IReadOnlyList<CoreAgent> agentsInBaseOrder, bool agentOrderIsUserDefined)
    {
        if (agentOrderIsUserDefined)
        {
            return agentsInBaseOrder;
        }

        return agentsInBaseOrder
            .OrderByDescending(a => a.GuaranteedHours)
            .ToList();
    }

    private async Task<IReadOnlyDictionary<Guid, double>> LoadCurrentHoursAsync(
        WizardContextRequest request, CancellationToken ct)
    {
        var (periodStart, _) = await _periodHoursService.GetPeriodBoundariesAsync(request.PeriodFrom);

        // Extend the prior-load range with the boundary days BEFORE PeriodFrom when the request asks
        // for boundary context. This lets MaxWeeklyHours-style validators see hours an agent already
        // accumulated in an adjacent payment period that share the same ISO week as PeriodFrom — e.g.
        // when PeriodFrom is the first day of a month that lands mid-week, the prior days of the same
        // ISO week belong to the previous month and would otherwise be invisible to the wizard.
        var contextDaysBefore = Math.Max(0, request.ContextDaysBefore);
        var boundaryStart = request.PeriodFrom.AddDays(-contextDaysBefore);
        var effectivePriorStart = periodStart < boundaryStart ? periodStart : boundaryStart;

        if (effectivePriorStart >= request.PeriodFrom)
        {
            return new Dictionary<Guid, double>();
        }

        var priorEnd = request.PeriodFrom.AddDays(-1);
        // Prior hours (strictly before PeriodFrom) are immutable history, so they are always read from
        // the REAL ledger (token = null), never the scenario token. In scenario mode the clone only
        // reaches PeriodFrom - BoundaryDays(14); a payment period that starts earlier (e.g. planning the
        // last week of a month) would otherwise return ZERO carry-in under the token-exact filter, making
        // the agent look under-target/"hungry" and under-triggering the hard max-hours cap.
        var priorHours = await _periodHoursService.GetPeriodHoursAsync(
            request.AgentIds.ToList(), effectivePriorStart, priorEnd, analyseToken: null);

        return priorHours.ToDictionary(
            kv => kv.Key,
            kv => (double)(kv.Value.Hours + kv.Value.Surcharges));
    }
}
