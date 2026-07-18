// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Hosted background service that periodically checks the marketplace for a newer published version
/// of the installed region package and applies it via <see cref="IRegionPackageUpdateRunner"/>.
/// Runs the first check after an initial delay, then every 24 hours; each cycle resolves its scoped
/// dependencies from a fresh DI scope, and any cycle failure is logged without stopping the service
/// or affecting application startup.
/// </summary>
/// <param name="scopeFactory">Creates a DI scope per cycle for the scoped update runner</param>
/// <param name="logger">Logger instance for diagnostic output</param>
using Klacks.Api.Application.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionPackageUpdateService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegionPackageUpdateService> _logger;

    public RegionPackageUpdateService(
        IServiceScopeFactory scopeFactory,
        ILogger<RegionPackageUpdateService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "RegionPackageUpdateService started, first check in {DelayMinutes} minute(s)",
            InitialDelay.TotalMinutes);

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RegionPackageUpdateService cancelled during startup delay; exiting cleanly.");
            return;
        }

        await RunCycleSafelyAsync(stoppingToken);

        using var timer = new PeriodicTimer(CheckInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCycleSafelyAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("RegionPackageUpdateService stopped");
    }

    private async Task RunCycleSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IRegionPackageUpdateRunner>();
            await runner.RunCycleAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(ex, "Region package update cycle failed; the service keeps running");
        }
    }
}
