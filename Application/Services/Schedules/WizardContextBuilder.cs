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

    public WizardContextBuilder(
        WizardAgentSnapshotBuilder agentBuilder,
        IWizardShiftBuilder shiftBuilder,
        IWizardHardConstraintBuilder hardConstraintBuilder,
        IPeriodHoursService periodHoursService,
        IClientContractDataProvider contractProvider)
    {
        _agentBuilder = agentBuilder;
        _shiftBuilder = shiftBuilder;
        _hardConstraintBuilder = hardConstraintBuilder;
        _periodHoursService = periodHoursService;
        _contractProvider = contractProvider;
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

        var defaults = await _contractProvider.GetEffectiveContractDataAsync(Guid.Empty, request.PeriodFrom);

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
            SchedulingMaxConsecutiveDays = defaults.MaxConsecutiveDays > 0 ? defaults.MaxConsecutiveDays : 6,
            SchedulingMinPauseHours = defaults.MinPauseHours > 0 ? (double)defaults.MinPauseHours : 11,
            SchedulingMaxOptimalGap = defaults.MaxOptimalGap > 0 ? (double)defaults.MaxOptimalGap : 2,
            SchedulingMaxDailyHours = defaults.MaxDailyHours > 0 ? (double)defaults.MaxDailyHours : 10,
            SchedulingMaxWeeklyHours = defaults.MaxWeeklyHours > 0 ? (double)defaults.MaxWeeklyHours : 50,
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
