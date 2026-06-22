// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that soft-deletes broadcast pending notes one day after their first
/// delivery, so a broadcast reaches every user within its lifetime and is then archived.
/// </summary>
/// <param name="scopeFactory">Factory for creating DI scopes in background tasks</param>
/// <param name="logger">Logger instance</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class PendingNoteBroadcastCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingNoteBroadcastCleanupBackgroundService> _logger;

    private static readonly TimeSpan BroadcastLifetime = TimeSpan.FromDays(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    public PendingNoteBroadcastCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingNoteBroadcastCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Pending note broadcast cleanup background service started");

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExpireBroadcastsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in pending note broadcast cleanup background service");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Pending note broadcast cleanup background service stopped");
    }

    private async Task ExpireBroadcastsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPendingUserNoteRepository>();

        var expired = await repository.ExpireBroadcastsAsync(BroadcastLifetime, stoppingToken);

        if (expired > 0)
        {
            _logger.LogInformation("Archived {Count} expired broadcast note(s)", expired);
        }
    }
}
