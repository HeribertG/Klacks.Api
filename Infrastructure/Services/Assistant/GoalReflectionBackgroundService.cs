// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that runs IGoalReflectionService.RunReflectionCycleAsync once per
/// configured interval (default 24 hours) so Klacksy can surface self-directed goal candidates without
/// a human trigger. Phase 1 is shadow mode: this service only owns the schedule, the reflection
/// service itself decides what happens with a cycle's candidates. Disabled by default via
/// BackgroundServiceOptions.GoalReflection — when disabled it logs once and never polls.
/// First run is delayed so the application is fully warmed up before the first cycle.
/// </summary>
/// <param name="serviceProvider">Creates a scoped DI provider per reflection cycle.</param>
/// <param name="options">Feature flag and cadence for this service.</param>
/// <param name="logger">Structured log per cycle.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class GoalReflectionBackgroundService : BackgroundService
{
    private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<GoalReflectionBackgroundService> _logger;

    public GoalReflectionBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<GoalReflectionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.GoalReflection)
        {
            _logger.LogInformation("GoalReflectionBackgroundService is disabled via configuration");
            return;
        }

        _logger.LogInformation(
            "GoalReflectionBackgroundService started, first run in {Delay}min", StartupDelay.TotalMinutes);

        try
        {
            await Task.Delay(StartupDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("GoalReflectionBackgroundService cancelled during startup delay; exiting cleanly.");
            return;
        }

        await RunCycleAsync(stoppingToken);

        var interval = TimeSpan.FromHours(_options.GoalReflectionIntervalHours);
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

        _logger.LogInformation("GoalReflectionBackgroundService stopped");
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var reflectionService = scope.ServiceProvider.GetRequiredService<IGoalReflectionService>();
            var candidateCount = await reflectionService.RunReflectionCycleAsync(cancellationToken);
            _logger.LogInformation(
                "GoalReflectionBackgroundService - reflection cycle completed in {Ms}ms, {Count} candidate(s)",
                sw.ElapsedMilliseconds, candidateCount);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "GoalReflectionBackgroundService - reflection cycle failed after {Ms}ms", sw.ElapsedMilliseconds);
        }
    }
}
