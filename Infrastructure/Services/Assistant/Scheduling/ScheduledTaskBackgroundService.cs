// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Drives the recurring (cron) task feature. Ticks once a minute, and on each tick resolves a fresh DI
/// scope and asks <see cref="IScheduledTaskRunner"/> to process every due task. The runner owns the
/// fire/skip decision, the atomic claim, execution, delivery and outcome recording; this host only
/// provides the clock, the scope and cooperative cancellation. Failures in a tick are logged and the
/// loop continues.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant.Scheduling;

public sealed class ScheduledTaskBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledTaskBackgroundService> _logger;

    public ScheduledTaskBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledTaskBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scheduled-task service started; tick every {Seconds}s.", TickInterval.TotalSeconds);

        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IScheduledTaskRunner>();
                await runner.RunDueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled-task tick failed");
            }
        }
    }
}
