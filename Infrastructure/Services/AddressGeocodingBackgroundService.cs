// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Background worker that geocodes client addresses off the request path (bulk employee import). Each
/// queued address id is processed in its own DI scope through IAddressGeocodingProcessor, followed by a
/// pause so interactive address saves still get their turn at the shared, rate-limited geocoder.
/// The in-memory channel is bounded to two full imports; TryQueue never waits and refuses an id while the
/// channel is full (FullMode Wait, so a refused write is reported as false rather than silently dropped). It is
/// not persisted and there is deliberately no resume scan at startup (addresses carry no "attempted"
/// flag, a scan would sweep every old address), so ids still queued at a restart are lost and those
/// addresses simply stay without coordinates. Failures are logged and never stop the loop.
/// </summary>

using System.Threading.Channels;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services;

public class AddressGeocodingBackgroundService : BackgroundService, IAddressGeocodingQueue
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AddressGeocodingBackgroundService> _logger;
    private readonly Channel<Guid> _channel;
    private readonly TimeSpan _pause;

    public AddressGeocodingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AddressGeocodingBackgroundService> logger)
        : this(serviceProvider, logger, TimeSpan.FromMilliseconds(ClientImportLimits.GeocodingPauseMilliseconds))
    {
    }

    public AddressGeocodingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AddressGeocodingBackgroundService> logger,
        TimeSpan pause)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _pause = pause;
        _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(ClientImportLimits.GeocodingQueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true
        });
    }

    public bool TryQueue(Guid addressId) => _channel.Writer.TryWrite(addressId);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AddressGeocodingBackgroundService started");

        try
        {
            await foreach (var addressId in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                await ProcessAsync(addressId, stoppingToken);
                await Task.Delay(_pause, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("AddressGeocodingBackgroundService stopping");
        }

        _logger.LogInformation("AddressGeocodingBackgroundService stopped");
    }

    private async Task ProcessAsync(Guid addressId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<IAddressGeocodingProcessor>();
            await processor.ProcessAsync(addressId, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error geocoding address {AddressId}", addressId);
        }
    }
}
