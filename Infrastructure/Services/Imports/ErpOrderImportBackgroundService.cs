// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Drives the ERP order import feature. Ticks once a minute and resolves a fresh DI scope per
/// tick; IErpOrderImportRunner owns the cron due/skip decision and the actual import work. Kept
/// as its own service rather than folded into the Klacksy scheduled-task feature, which is a
/// user-facing chat reminder/skill runner, not a system integration job.
/// </summary>
using Klacks.Api.Domain.Interfaces.Imports;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Imports;

public sealed class ErpOrderImportBackgroundService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ErpOrderImportBackgroundService> _logger;

    public ErpOrderImportBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ErpOrderImportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ERP order import service started; tick every {Seconds}s.", TickInterval.TotalSeconds);

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
                var runner = scope.ServiceProvider.GetRequiredService<IErpOrderImportRunner>();
                await runner.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERP order import tick failed");
            }
        }
    }
}
