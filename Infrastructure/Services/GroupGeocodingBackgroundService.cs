// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Background worker that resolves a group's coordinates off the request path: callers (e.g. group
/// creation) enqueue a group id, and each is processed in its own DI scope via
/// <see cref="IGroupLocationResolver"/>. Failures are logged and never crash the loop. Enqueue only
/// after the group's create transaction has committed, otherwise the worker may read it before it is
/// visible. The queue itself is an in-memory Channel with no persistence, so a process restart while
/// items are still pending silently loses them — on startup this worker resumes every group that was
/// never actually run through the resolver (GeocodingAttempted still false, no coordinates yet),
/// recovering exactly that loss without re-classifying groups that already have a real outcome.
/// With geocoding switched off by the operator nothing is queued and nothing is resumed: the groups keep
/// GeocodingAttempted == false and are picked up on the first start after geocoding is switched back on.
/// </summary>
/// <param name="geoServicesPolicy">Effective geo service configuration (is geocoding enabled)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Interfaces.GeoServices;
using System.Threading.Channels;

namespace Klacks.Api.Infrastructure.Services;

public class GroupGeocodingBackgroundService : BackgroundService, IGroupGeocodingQueue
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GroupGeocodingBackgroundService> _logger;
    private readonly Channel<Guid> _channel;
    private readonly IGeoServicesPolicy _geoServicesPolicy;

    public GroupGeocodingBackgroundService(
        IServiceProvider serviceProvider,
        IGeoServicesPolicy geoServicesPolicy,
        ILogger<GroupGeocodingBackgroundService> logger)
    {
        _geoServicesPolicy = geoServicesPolicy;
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
        if (!_geoServicesPolicy.Geocoding.IsEnabled)
        {
            _logger.LogDebug("Geocoding is disabled, group {GroupId} is not queued", groupId);
            return;
        }

        if (!_channel.Writer.TryWrite(groupId))
        {
            _logger.LogWarning("Failed to queue group {GroupId} for geocoding", groupId);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GroupGeocodingBackgroundService started");

        if (_geoServicesPolicy.Geocoding.IsEnabled)
        {
            await ResumeUnattemptedGroupsAsync(stoppingToken);
        }
        else
        {
            _logger.LogWarning("Group geocoding is disabled by configuration, groups without coordinates are not resumed");
        }

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

    private async Task ResumeUnattemptedGroupsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var groupRepository = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
        var pendingGroupIds = await groupRepository.GetUnattemptedGeocodingCandidateIdsAsync(cancellationToken);

        foreach (var groupId in pendingGroupIds)
        {
            Queue(groupId);
        }

        if (pendingGroupIds.Count > 0)
        {
            _logger.LogInformation(
                "Resumed {Count} group(s) left over from a previous run for geocoding", pendingGroupIds.Count);
        }
    }
}
