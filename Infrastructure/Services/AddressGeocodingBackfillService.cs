// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Background worker that gives addresses without coordinates their locality coordinates. It replaces the former
/// in-memory import queue: nothing lives in memory, every pass reads what is due from the database (requested
/// addresses first, then never attempted ones, then expired retries), so nothing is lost on a restart and an address
/// whose postcode, city or country changed is picked up again. A single worker paces itself (one pass at a time,
/// AddressGeocodingBackfillRunner pauses after each real Nominatim request); after a full batch the next pass
/// follows at once, otherwise it waits for the scan interval. Each pass runs in its own DI scope (the worker is a
/// singleton; scoped services, ICompanyClock included, are never captured). Failures are logged and never stop the
/// loop. Run it on exactly one instance when scaling out (BackgroundServices:AddressGeocoding).
/// </summary>

using Klacks.Api.Application.Interfaces.Geocoding;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services;

public class AddressGeocodingBackfillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AddressGeocodingBackfillService> _logger;
    private readonly TimeSpan _startupDelay;
    private readonly TimeSpan _scanInterval;

    public AddressGeocodingBackfillService(
        IServiceScopeFactory scopeFactory,
        ILogger<AddressGeocodingBackfillService> logger)
        : this(
            scopeFactory,
            logger,
            TimeSpan.FromSeconds(AddressGeocodingLimits.StartupDelaySeconds),
            TimeSpan.FromMinutes(AddressGeocodingLimits.ScanIntervalMinutes))
    {
    }

    internal AddressGeocodingBackfillService(
        IServiceScopeFactory scopeFactory,
        ILogger<AddressGeocodingBackfillService> logger,
        TimeSpan startupDelay,
        TimeSpan scanInterval)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _startupDelay = startupDelay;
        _scanInterval = scanInterval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AddressGeocodingBackfillService started");

        try
        {
            await Task.Delay(_startupDelay, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!await RunPassAsync(stoppingToken))
                {
                    await Task.Delay(_scanInterval, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("AddressGeocodingBackfillService stopping");
        }

        _logger.LogInformation("AddressGeocodingBackfillService stopped");
    }

    private async Task<bool> RunPassAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var runner = scope.ServiceProvider.GetRequiredService<IAddressGeocodingBackfillRunner>();
            return await runner.RunBatchAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Address geocoding pass failed; retried at the next interval");
            return false;
        }
    }
}
