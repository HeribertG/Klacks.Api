// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Restores the learned skill rerank threshold shortly after startup and then sharpens it every few hours, each
/// time in its own scope and off the hot path. Failures are logged and swallowed: the worst case is that the
/// threshold stays where it is. The restore delay is short on purpose, because until it runs the configured start
/// value is in force instead of the learned one.
/// </summary>
/// <param name="serviceProvider">Used to create a scope per iteration for the scoped tuner.</param>
/// <param name="logger">Diagnostic logging of iteration failures.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class SkillRerankTuningBackgroundService : BackgroundService
{
    private static readonly TimeSpan RestoreDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkillRerankTuningBackgroundService> _logger;

    public SkillRerankTuningBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SkillRerankTuningBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(RestoreDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        await RunAsync((tuner, ct) => tuner.RestoreAsync(ct), "restore", stoppingToken);

        using var timer = new PeriodicTimer(Interval);
        while (await WaitForNextTickAsync(timer, stoppingToken))
        {
            await RunAsync((tuner, ct) => tuner.TuneAsync(ct), "tuning", stoppingToken);
        }
    }

    private async Task RunAsync(
        Func<ISkillRerankThresholdTuner, CancellationToken, Task> step, string stepName, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var tuner = scope.ServiceProvider.GetRequiredService<ISkillRerankThresholdTuner>();
            await step(tuner, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Skill rerank threshold {Step} failed.", stepName);
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
