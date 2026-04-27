// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Assistant;

/// <summary>
/// Runs LLM model discovery at startup (after 30s delay) and every N hours.
/// Uses a DI scope per run since ILLMModelSyncService is scoped.
/// </summary>
public class LLMModelSyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LLMModelSyncBackgroundService> _logger;
    private readonly BackgroundServiceOptions _options;

    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);

    public LLMModelSyncBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<BackgroundServiceOptions> options,
        ILogger<LLMModelSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.LLMModelSync)
        {
            _logger.LogInformation("LLMModelSyncBackgroundService is disabled via configuration");
            return;
        }

        _logger.LogInformation("LLMModelSyncBackgroundService started, first run in {Delay}s", StartupDelay.TotalSeconds);

        await Task.Delay(StartupDelay, stoppingToken);

        await RunSyncAsync(stoppingToken);

        var interval = TimeSpan.FromHours(_options.LLMModelSyncIntervalHours);
        using var timer = new PeriodicTimer(interval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunSyncAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("LLMModelSyncBackgroundService stopped");
    }

    private async Task RunSyncAsync(CancellationToken cancellationToken)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ILLMModelSyncService>();
            await syncService.SyncAllProvidersAsync(cancellationToken);
            _logger.LogInformation("LLMModelSyncBackgroundService - Sync cycle completed in {Ms}ms", sw.ElapsedMilliseconds);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "LLMModelSyncBackgroundService - Sync cycle failed after {Ms}ms", sw.ElapsedMilliseconds);
        }
    }
}
