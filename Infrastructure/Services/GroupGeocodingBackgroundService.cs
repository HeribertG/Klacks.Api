// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background worker that resolves a group's coordinates off the request path: callers (e.g. group
/// creation) enqueue a group id, and each is processed in its own DI scope via
/// <see cref="IGroupLocationResolver"/>. Failures are logged and never crash the loop. Enqueue only
/// after the group's create transaction has committed, otherwise the worker may read it before it is
/// visible.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using System.Threading.Channels;

namespace Klacks.Api.Infrastructure.Services;

public class GroupGeocodingBackgroundService : BackgroundService, IGroupGeocodingQueue
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GroupGeocodingBackgroundService> _logger;
    private readonly Channel<Guid> _channel;

    public GroupGeocodingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<GroupGeocodingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _channel = Channel.CreateBounded<Guid>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });
    }

    public void Queue(Guid groupId)
    {
        if (!_channel.Writer.TryWrite(groupId))
        {
            _logger.LogWarning("Failed to queue group {GroupId} for geocoding", groupId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GroupGeocodingBackgroundService started");

        try
        {
            await foreach (var groupId in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var resolver = scope.ServiceProvider.GetRequiredService<IGroupLocationResolver>();
                    var result = await resolver.ResolveAsync(groupId, stoppingToken);

                    _logger.LogInformation(
                        "Group geocoding for {GroupId} ('{GroupName}') -> {Outcome}",
                        result.GroupId, result.GroupName, result.Outcome);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error geocoding group {GroupId}", groupId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown - ignore
        }

        _logger.LogInformation("GroupGeocodingBackgroundService stopped");
    }
}
