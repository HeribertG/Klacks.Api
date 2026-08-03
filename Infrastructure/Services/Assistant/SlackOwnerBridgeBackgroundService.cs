// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service that runs ISlackOwnerBridgeService.RunCycleAsync once per configured
/// interval (default 15s) so a Slack message from the owner reaches Klacksy's LLM even while no
/// browser session is connected. This service only owns the schedule and the feature flag — the
/// bridge service itself decides what a cycle does. Disabled by default via
/// BackgroundServiceOptions.SlackOwnerBridge — when disabled it logs once and never polls.
/// </summary>
/// <param name="serviceProvider">Creates a scoped DI provider per bridge cycle.</param>
/// <param name="options">Feature flag and poll interval for this service.</param>
/// <param name="logger">Structured log per cycle.</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class SlackOwnerBridgeBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BackgroundServiceOptions _options;
    private readonly ILogger<SlackOwnerBridgeBackgroundService> _logger;

    public SlackOwnerBridgeBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<SlackOwnerBridgeBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.SlackOwnerBridge)
        {
            _logger.LogInformation("SlackOwnerBridgeBackgroundService is disabled via configuration");
            return;
        }

        var interval = TimeSpan.FromSeconds(_options.SlackOwnerBridgeIntervalSeconds);
        _logger.LogInformation("SlackOwnerBridgeBackgroundService started, polling every {Interval}", interval);

        using var timer = new PeriodicTimer(interval);

        try
        {
            do
            {
                await RunCycleAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("SlackOwnerBridgeBackgroundService stopped");
    }

    internal async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var bridgeService = scope.ServiceProvider.GetRequiredService<ISlackOwnerBridgeService>();
            var processedCount = await bridgeService.RunCycleAsync(cancellationToken);
            if (processedCount > 0)
            {
                _logger.LogInformation(
                    "SlackOwnerBridgeBackgroundService - cycle completed in {Ms}ms, {Count} message(s) processed",
                    sw.ElapsedMilliseconds, processedCount);
            }
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "SlackOwnerBridgeBackgroundService - cycle failed after {Ms}ms", sw.ElapsedMilliseconds);
        }
    }
}
