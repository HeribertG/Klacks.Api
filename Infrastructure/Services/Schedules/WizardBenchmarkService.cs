// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default implementation of IWizardBenchmarkService.
/// Composes the same context-builder + TokenEvolutionLoop as WizardJobRunner, but runs synchronously
/// and returns a rich metrics snapshot instead of streaming SignalR progress events.
/// Supports optional DB persistence (WizardTrainingRun) and DB-auto-population for the background
/// autotraining service.
/// </summary>
/// <param name="scopeFactory">Creates an isolated DI scope per benchmark (fresh DbContext, repositories).</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Application.Constants;
using System.Diagnostics;
using System.Text.Json;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Klacks.ScheduleOptimizer.TokenEvolution;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class WizardBenchmarkService : IWizardBenchmarkService
{
    private static readonly JsonSerializerOptions ConfigJsonOptions = new()
    {
        WriteIndented = false,
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AutofillStartGuard _startGuard;

    public WizardBenchmarkService(IServiceScopeFactory scopeFactory, AutofillStartGuard startGuard)
    {
        _scopeFactory = scopeFactory;
        _startGuard = startGuard;
    }

    public async Task<WizardBenchmarkResponse> RunAsync(WizardContextRequest request, CancellationToken ct)
    {
        // The benchmark endpoint runs synchronously inside the request, so an oversized selection would
        // hold a request thread for as long as the engine needs. It goes through the same guard as the
        // real runs - checked before any scope is created, so an oversized request fails without cost.
        _startGuard.EnsureWithinLimits(
            AutofillFamily.Benchmark, request.AgentIds.Count, request.ShiftIds?.Count ?? 0,
            request.PeriodFrom, request.PeriodUntil);

        var benchmarkId = Guid.NewGuid();
        _startGuard.AcquireRunLock(
            AutofillFamily.Benchmark, request.PeriodFrom, request.PeriodUntil,
            request.AnalyseToken, request.AgentIds, benchmarkId);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            using var budgetCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            budgetCts.CancelAfter(AutofillLimits.BenchmarkMaxRuntime + AutofillLimits.BenchmarkHardCancelGrace);

            var builder = scope.ServiceProvider.GetRequiredService<IWizardContextBuilder>();
            var wizardContext = await builder.BuildContextAsync(request, budgetCts.Token);
            return await RunCoreAsync(wizardContext, request, budgetCts.Token, AutofillLimits.BenchmarkMaxRuntime);
        }
        finally
        {
            _startGuard.ReleaseRunLock(benchmarkId);
        }
    }

    public async Task<WizardBenchmarkResponse> RunAndPersistAsync(
        WizardContextRequest request, string source, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<IWizardContextBuilder>();
        var repo = scope.ServiceProvider.GetRequiredService<IWizardTrainingRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var wizardContext = await builder.BuildContextAsync(request, ct);
        var response = await RunCoreAsync(wizardContext, request, ct);

        await repo.AddAsync(BuildTrainingRun(response, wizardContext, request, source), ct);
        await unitOfWork.CompleteAsync();

        return response;
    }

    public async Task<WizardBenchmarkResponse?> RunAutoPopulatedAsync(
        int maxAgents,
        int maxShifts,
        DateOnly periodFrom,
        DateOnly periodUntil,
        WizardTrainingOverrides? overrides,
        string source,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var agentIds = await db.Client
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Id)
            .Take(maxAgents)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var shiftIds = await db.Shift
            .AsNoTracking()
            .Where(s => !s.IsDeleted
                && s.FromDate <= periodUntil
                && (s.UntilDate == null || s.UntilDate >= periodFrom))
            .OrderBy(s => s.Id)
            .Take(maxShifts)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (agentIds.Count == 0 || shiftIds.Count == 0)
        {
            return null;
        }

        var request = new WizardContextRequest(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            ShiftIds: shiftIds,
            AnalyseToken: null,
            TrainingOverrides: overrides);

        return await RunAndPersistAsync(request, source, ct);
    }

    /// <summary>
    /// Runs one benchmark. The cap is a ceiling, never an extension: a caller asking for less keeps its
    /// own budget. Training overrides cannot raise it - they carry no MaxRuntime - and the linked token
    /// of the caller is the hard backstop behind this soft limit.
    /// </summary>
    /// <param name="wizardContext">Context the run scores against.</param>
    /// <param name="request">Original request, for its training overrides.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="maxRuntimeCap">Upper bound for the loop's soft budget; null leaves it untouched.</param>
    internal static async Task<WizardBenchmarkResponse> RunCoreAsync(
        CoreWizardContext wizardContext,
        WizardContextRequest request,
        CancellationToken ct,
        TimeSpan? maxRuntimeCap = null)
    {
        var baseline = new TokenEvolutionConfig
        {
            RandomSeed = Random.Shared.Next(),
        };
        var config = request.TrainingOverrides?.Apply(baseline) ?? baseline;

        if (maxRuntimeCap is { } cap)
        {
            config = config with
            {
                MaxRuntime = config.MaxRuntime is { } requested && requested < cap ? requested : cap,
            };
        }

        var loop = TokenEvolutionLoop.Create();

        var stopwatch = Stopwatch.StartNew();
        var best = loop.Run(wizardContext, config, progress: null, ct);
        stopwatch.Stop();

        return await Task.FromResult(BuildResponse(best, wizardContext, config, stopwatch.ElapsedMilliseconds));
    }

    private static WizardBenchmarkResponse BuildResponse(
        CoreScenario best,
        CoreWizardContext context,
        TokenEvolutionConfig config,
        long durationMs)
    {
        var nonLocked = best.Tokens.Where(t => !t.IsLocked).ToList();

        var availableSlots = context.Shifts.Count;
        var coverage = availableSlots == 0
            ? 0d
            : Math.Min(1d, (double)nonLocked.Count / availableSlots);

        var clientDayDuplicates = nonLocked
            .GroupBy(t => (t.AgentId, t.Date))
            .Count(g => g.Count() > 1);

        var capacityPerSlot = context.Shifts
            .GroupBy(s => (s.Id, s.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        var tokensPerSlot = nonLocked
            .GroupBy(t => (t.ShiftRefId.ToString(), t.Date.ToString("yyyy-MM-dd")))
            .ToDictionary(g => g.Key, g => g.Count());

        var metrics = ComputeSlotMetrics(capacityPerSlot, tokensPerSlot);
        var shiftCoverage = availableSlots == 0 ? 0d : (double)metrics.Covered / availableSlots;

        var agentsInContext = context.Agents.Count;
        var agentsShiftCapable = context.Agents.Count(a => a.PerformsShiftWork);
        var slotsFillableByData = context.Shifts.Count(s =>
            SlotLooksFillable(s, agentsShiftCapable, agentsInContext));
        var theoreticalMax = availableSlots == 0 ? 0d : (double)slotsFillableByData / availableSlots;

        return new WizardBenchmarkResponse(
            DurationMs: durationMs,
            FinalHardViolations: best.FitnessStage0,
            FinalStage1Completion: best.FitnessStage1,
            FinalStage2Score: best.FitnessStage2,
            TokenCount: nonLocked.Count,
            AvailableShiftSlots: availableSlots,
            CoverageRatio: coverage,
            ClientDayDuplicates: clientDayDuplicates,
            DistinctAgents: nonLocked.Select(t => t.AgentId).Distinct().Count(),
            DistinctShifts: nonLocked.Select(t => t.ShiftRefId).Distinct().Count(),
            DistinctDates: nonLocked.Select(t => t.Date).Distinct().Count(),
            CoveredShiftSlots: metrics.Covered,
            OverstaffingCount: metrics.Overstaffing,
            UndersupplyCount: metrics.Undersupply,
            ShiftCoverageRatio: shiftCoverage,
            AgentsInContext: agentsInContext,
            AgentsShiftCapable: agentsShiftCapable,
            SlotsFillableByData: slotsFillableByData,
            TheoreticalMaxCoverage: theoreticalMax,
            EffectiveConfig: BuildEffectiveConfigDto(config));
    }

    private record SlotMetrics(int Covered, int Overstaffing, int Undersupply);

    private static SlotMetrics ComputeSlotMetrics(
        Dictionary<(string, string), int> capacityPerSlot,
        Dictionary<(string, string), int> tokensPerSlot)
    {
        var covered = 0;
        var overstaffing = 0;
        var undersupply = 0;

        foreach (var slot in capacityPerSlot)
        {
            var assigned = tokensPerSlot.GetValueOrDefault(slot.Key, 0);
            covered += Math.Min(slot.Value, assigned);
            if (assigned > slot.Value)
                overstaffing += assigned - slot.Value;
            else
                undersupply += slot.Value - assigned;
        }

        foreach (var orphan in tokensPerSlot)
        {
            if (!capacityPerSlot.ContainsKey(orphan.Key))
                overstaffing += orphan.Value;
        }

        return new SlotMetrics(covered, overstaffing, undersupply);
    }

    private static WizardEffectiveConfigDto BuildEffectiveConfigDto(TokenEvolutionConfig config) =>
        new(
            PopulationSize: config.PopulationSize,
            MaxGenerations: config.MaxGenerations,
            TournamentK: config.TournamentK,
            MutationRate: config.MutationRate,
            CrossoverRate: config.CrossoverRate,
            ElitismCount: config.ElitismCount,
            MutationWeightSwap: config.MutationWeightSwap,
            MutationWeightSplit: config.MutationWeightSplit,
            MutationWeightMerge: config.MutationWeightMerge,
            MutationWeightReassign: config.MutationWeightReassign,
            MutationWeightRepair: config.MutationWeightRepair,
            EarlyStopNoImprovementGenerations: config.EarlyStopNoImprovementGenerations,
            RandomSeed: config.RandomSeed);

    /// <summary>
    /// Lightweight upper-bound heuristic: a slot is fillable if at least one agent in the context
    /// could potentially take it, considering only the shift-work flag. Non-early slots require at
    /// least one agent with <c>PerformsShiftWork=true</c>; early slots always pass.
    /// Intentionally ignores weekday flags, break blockers and keywords — that would double the cost
    /// and only refines the upper bound marginally.
    /// </summary>
    private static bool SlotLooksFillable(CoreShift slot, int agentsShiftCapable, int agentsInContext)
    {
        if (agentsInContext == 0)
        {
            return false;
        }

        var shiftTypeIndex = ShiftTypeInference.FromSpanString(slot.StartTime, slot.EndTime);
        return shiftTypeIndex == 0 || agentsShiftCapable > 0;
    }

    private static WizardTrainingRun BuildTrainingRun(
        WizardBenchmarkResponse response,
        CoreWizardContext context,
        WizardContextRequest request,
        string source)
    {
        return new WizardTrainingRun
        {
            Id = Guid.NewGuid(),
            Source = source,
            ConfigJson = JsonSerializer.Serialize(response.EffectiveConfig, ConfigJsonOptions),
            DurationMs = response.DurationMs,
            Stage0Violations = response.FinalHardViolations,
            Stage1Completion = response.FinalStage1Completion,
            Stage2Score = response.FinalStage2Score,
            TokenCount = response.TokenCount,
            AvailableShiftSlots = response.AvailableShiftSlots,
            CoverageRatio = response.CoverageRatio,
            ClientDayDuplicates = response.ClientDayDuplicates,
            AgentsCount = context.Agents.Count,
            ShiftsCount = request.ShiftIds?.Count ?? 0,
            PeriodDays = (request.PeriodUntil.DayNumber - request.PeriodFrom.DayNumber) + 1,
        };
    }
}
