// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that aborts AgentPlan rows stuck in PlanStatus.PausedForApproval for
/// longer than the configured timeout. Unattended runs (nightly, no open chat session) inevitably leave
/// some plans waiting for a human approval that never comes; without an expiry those plans would block
/// their resources and clutter the plan overview forever. Disabled via
/// BackgroundServiceOptions.PlanApprovalTimeout — see that option's XML doc for why the default is ON,
/// unlike most services in this folder. First run is delayed so the application is fully warmed up
/// before the first sweep.
/// </summary>
/// <param name="serviceProvider">Creates a scoped DI provider per sweep cycle.</param>
/// <param name="options">Feature flag, timeout window and cadence for this service.</param>
/// <param name="logger">Structured log per cycle.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Services.Assistant.Planning;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PlanApprovalTimeoutBackgroundService : BackgroundService
{
    private const int MaxPlansPerCycle = 200;
    private const string TimeoutReasonTemplate =
        "Approval window of {0} day(s) expired without a decision; the plan was aborted automatically.";

    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<PlanApprovalTimeoutBackgroundService> _logger;

    public PlanApprovalTimeoutBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<PlanApprovalTimeoutBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.PlanApprovalTimeout)
        {
            _logger.LogInformation("PlanApprovalTimeoutBackgroundService is disabled via configuration");
            return;
        }

        _logger.LogInformation(
            "PlanApprovalTimeoutBackgroundService started, first run in {Delay}min", StartupDelay.TotalMinutes);

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("PlanApprovalTimeoutBackgroundService cancelled during startup delay; exiting cleanly.");
            return;
        }

        await RunCycleAsync(stoppingToken);

        var interval = TimeSpan.FromHours(_options.PlanApprovalTimeoutIntervalHours);
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

        _logger.LogInformation("PlanApprovalTimeoutBackgroundService stopped");
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var planRepository = scope.ServiceProvider.GetRequiredService<IAgentPlanRepository>();

            var timeoutDays = _options.PlanApprovalTimeoutDays;
            var updatedBeforeUtc = DateTime.UtcNow.AddDays(-timeoutDays);
            var stalePlans = await planRepository.GetStalePausedForApprovalAsync(
                updatedBeforeUtc, MaxPlansPerCycle, cancellationToken);

            var abortedIds = new List<Guid>();
            foreach (var plan in stalePlans)
            {
                try
                {
                    plan.Status = PlanStatus.Aborted;
                    plan.LastErrorMessage = string.Format(TimeoutReasonTemplate, timeoutDays);
                    await planRepository.UpdateAsync(plan, cancellationToken);
                    abortedIds.Add(plan.Id);
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "PlanApprovalTimeoutBackgroundService - failed to abort plan {PlanId}", plan.Id);
                }
            }

            _logger.LogInformation(
                "PlanApprovalTimeoutBackgroundService - sweep completed in {Ms}ms, {Count} plan(s) aborted: {PlanIds}",
                sw.ElapsedMilliseconds, abortedIds.Count, abortedIds);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "PlanApprovalTimeoutBackgroundService - sweep failed after {Ms}ms", sw.ElapsedMilliseconds);
        }
    }
}
