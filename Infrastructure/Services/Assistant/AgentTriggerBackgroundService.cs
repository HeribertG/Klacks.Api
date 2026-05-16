// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that runs all registered IAgentTriggerDetectors once per hour
/// and dispatches every emitted event through IAgentTriggerService (rate-limited per user).
/// First run is delayed 2 minutes so the application is fully warmed up before scanning.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per tick.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class AgentTriggerBackgroundService : BackgroundService
{
    private const int ScanIntervalMinutes = 60;
    private const int FirstRunDelayMinutes = 2;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentTriggerBackgroundService> _logger;

    public AgentTriggerBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentTriggerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Agent trigger background service started");

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(FirstRunDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ScanTickAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent trigger scan tick failed");
                }

                await Task.Delay(TimeSpan.FromMinutes(ScanIntervalMinutes), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Agent trigger background service stopped");
    }

    private async Task ScanTickAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var detectors = scope.ServiceProvider.GetServices<IAgentTriggerDetector>().ToList();
        if (detectors.Count == 0)
        {
            return;
        }

        var triggerService = scope.ServiceProvider.GetRequiredService<IAgentTriggerService>();
        var totalEvents = 0;

        foreach (var detector in detectors)
        {
            var events = await detector.DetectAsync(cancellationToken);
            foreach (var triggerEvent in events)
            {
                await triggerService.OnEventAsync(triggerEvent, cancellationToken);
                totalEvents++;
            }
        }

        _logger.LogDebug("Agent trigger scan tick complete — {Count} detector(s), {Events} event(s) dispatched",
            detectors.Count, totalEvents);
    }
}
