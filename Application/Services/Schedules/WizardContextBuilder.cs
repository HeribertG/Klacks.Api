// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardContextBuilder"/>.
/// Composes the agent snapshot, shift expansion and hard-constraint sub-builders into a full
/// <see cref="CoreWizardContext"/> for the GA to consume.
/// </summary>
/// <param name="agentBuilder">Builder that produces CoreAgent and CoreContractDay snapshots</param>
/// <param name="shiftBuilder">Builder that expands shift definitions to per-day slots</param>
/// <param name="hardConstraintBuilder">Builder that loads commands, preferences, breaks and locked works</param>
/// <param name="periodHoursService">Provider for the hours already worked in the active period</param>
public sealed class WizardContextBuilder : IWizardContextBuilder
{
    private readonly WizardAgentSnapshotBuilder _agentBuilder;
    private readonly IWizardShiftBuilder _shiftBuilder;
    private readonly IWizardHardConstraintBuilder _hardConstraintBuilder;
    private readonly IPeriodHoursService _periodHoursService;

    public WizardContextBuilder(
        WizardAgentSnapshotBuilder agentBuilder,
        IWizardShiftBuilder shiftBuilder,
        IWizardHardConstraintBuilder hardConstraintBuilder,
        IPeriodHoursService periodHoursService)
    {
        _agentBuilder = agentBuilder;
        _shiftBuilder = shiftBuilder;
        _hardConstraintBuilder = hardConstraintBuilder;
        _periodHoursService = periodHoursService;
    }

    public async Task<CoreWizardContext> BuildContextAsync(WizardContextRequest request, CancellationToken ct)
    {
        var currentHours = await LoadCurrentHoursAsync(request, ct);

        var agentSnapshot = await _agentBuilder.BuildAsync(
            request.AgentIds, request.PeriodFrom, request.PeriodUntil, currentHours, ct);

        var shifts = await _shiftBuilder.BuildAsync(
            request.ShiftIds, request.PeriodFrom, request.PeriodUntil, ct);

        var hardConstraints = await _hardConstraintBuilder.BuildAsync(
            request.AgentIds, request.PeriodFrom, request.PeriodUntil, request.AnalyseToken, ct);

        var firstAgent = agentSnapshot.Agents.FirstOrDefault();

        return new CoreWizardContext
        {
            PeriodFrom = request.PeriodFrom,
            PeriodUntil = request.PeriodUntil,
            Agents = agentSnapshot.Agents,
            Shifts = shifts,
            ContractDays = agentSnapshot.ContractDays,
            ScheduleCommands = hardConstraints.ScheduleCommands,
            ShiftPreferences = hardConstraints.ShiftPreferences,
            BreakBlockers = hardConstraints.BreakBlockers,
            LockedWorks = hardConstraints.LockedWorks,
            ExistingWorkBlockers = hardConstraints.ExistingWorkBlockers,
            SchedulingMaxConsecutiveDays = firstAgent?.MaxConsecutiveDays ?? 6,
            SchedulingMinPauseHours = firstAgent?.MinRestHours ?? 11,
            SchedulingMaxOptimalGap = firstAgent?.MaxOptimalGap ?? 2,
            SchedulingMaxDailyHours = firstAgent?.MaxDailyHours ?? 10,
            SchedulingMaxWeeklyHours = firstAgent?.MaxWeeklyHours ?? 50,
            AnalyseToken = request.AnalyseToken,
        };
    }

    private async Task<IReadOnlyDictionary<Guid, double>> LoadCurrentHoursAsync(
        WizardContextRequest request, CancellationToken ct)
    {
        var (periodStart, _) = await _periodHoursService.GetPeriodBoundariesAsync(request.PeriodFrom);
        if (periodStart >= request.PeriodFrom)
        {
            return new Dictionary<Guid, double>();
        }

        var priorEnd = request.PeriodFrom.AddDays(-1);
        var priorHours = await _periodHoursService.GetPeriodHoursAsync(
            request.AgentIds.ToList(), periodStart, priorEnd, request.AnalyseToken);

        return priorHours.ToDictionary(
            kv => kv.Key,
            kv => (double)kv.Value.Hours);
    }
}
