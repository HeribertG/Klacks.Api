// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that re-attempts unattended execution for approved GoalCandidates whose
/// drafted plan never ran because a brake was temporarily closed at draft time (see
/// docs/superpowers/specs/2026-07-28-klacksy-selbstgesteuerte-ziele-design.md, section "Kein
/// Wiederholungsversuch"). Without this sweep, a candidate whose plan was drafted while
/// GoalReflectionExecution was still off - or while every admin's autonomy level was temporarily below
/// Autonomous - is stuck forever: GoalPlanDraftService refuses to draft a second plan once PlanId is
/// set, and nothing outside the original approval flow ever calls execution again. Each cycle re-checks
/// the linked AgentPlan's own status and only re-attempts candidates whose plan is still
/// PlanStatus.Drafting - the one status that means the plan has never actually started running. Every
/// other status (Executing, PausedForApproval, Completed, Aborted, Failed) is left untouched; this
/// service must never re-start a plan a second time. IGoalPlanExecutionService.ExecuteForCandidateAsync
/// still evaluates its own five brakes for every candidate handed to it - this service does not
/// duplicate or bypass any of them, it only decides which candidates are worth asking again. Disabled
/// via BackgroundServiceOptions.GoalPlanExecutionRetry - see that option's XML doc for why the default
/// is ON. First run is delayed so the application is fully warmed up before the first sweep.
/// </summary>
/// <param name="serviceProvider">Creates a scoped DI provider per sweep cycle.</param>
/// <param name="options">Feature flag and cadence for this service.</param>
/// <param name="logger">Structured log per cycle.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class GoalPlanExecutionRetryBackgroundService : BackgroundService
{
    private const int MaxCandidatesPerCycle = 200;

    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<GoalPlanExecutionRetryBackgroundService> _logger;

    public GoalPlanExecutionRetryBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<GoalPlanExecutionRetryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.GoalPlanExecutionRetry)
        {
            _logger.LogInformation("GoalPlanExecutionRetryBackgroundService is disabled via configuration");
            return;
        }

        _logger.LogInformation(
            "GoalPlanExecutionRetryBackgroundService started, first run in {Delay}min", StartupDelay.TotalMinutes);

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "GoalPlanExecutionRetryBackgroundService cancelled during startup delay; exiting cleanly.");
            return;
        }

        await RunCycleAsync(stoppingToken);

        var interval = TimeSpan.FromHours(_options.GoalPlanExecutionRetryIntervalHours);
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCycleAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("GoalPlanExecutionRetryBackgroundService stopped");
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var goalCandidateRepository = scope.ServiceProvider.GetRequiredService<IGoalCandidateRepository>();
            var planRepository = scope.ServiceProvider.GetRequiredService<IAgentPlanRepository>();
            var executionService = scope.ServiceProvider.GetRequiredService<IGoalPlanExecutionService>();

            var candidates = await goalCandidateRepository.GetApprovedWithPlanAsync(
                MaxCandidatesPerCycle, cancellationToken);

            var startedCount = 0;
            var skippedCount = 0;

            foreach (var candidate in candidates)
            {
                try
                {
                    if (!await ShouldRetryAsync(candidate, planRepository, cancellationToken))
                    {
                        skippedCount++;
                        continue;
                    }

                    var started = await executionService.ExecuteForCandidateAsync(candidate.Id, cancellationToken);
                    if (started)
                    {
                        startedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(
                        ex, "GoalPlanExecutionRetryBackgroundService - failed to retry candidate {CandidateId}",
                        candidate.Id);
                    skippedCount++;
                }
            }

            _logger.LogInformation(
                "GoalPlanExecutionRetryBackgroundService - sweep completed in {Ms}ms, {Started} candidate(s) " +
                "retried, {Skipped} skipped",
                sw.ElapsedMilliseconds, startedCount, skippedCount);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "GoalPlanExecutionRetryBackgroundService - sweep failed after {Ms}ms", sw.ElapsedMilliseconds);
        }
    }

    private static async Task<bool> ShouldRetryAsync(
        GoalCandidate candidate,
        IAgentPlanRepository planRepository,
        CancellationToken cancellationToken)
    {
        if (candidate.PlanId is not { } planId)
        {
            return false;
        }

        var plan = await planRepository.GetByIdAsync(planId, cancellationToken);
        return plan is not null && plan.Status == PlanStatus.Drafting;
    }
}
