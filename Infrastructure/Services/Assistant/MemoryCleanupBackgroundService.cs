// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodic background service for memory cleanup: removes expired memories,
/// adjusts importance based on access patterns and archives inactive sessions.
/// </summary>
/// <param name="scopeFactory">Factory for DI scopes in background tasks</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class MemoryCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MemoryCleanupBackgroundService> _logger;

    private const int IntervalHours = 6;
    private const int StaleSessionDays = 30;

    public MemoryCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MemoryCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Memory cleanup background service started");

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in memory cleanup background service");
                }

                await Task.Delay(TimeSpan.FromHours(IntervalHours), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Memory cleanup background service stopped");
    }

    private async Task CleanupAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var memoryRepository = scope.ServiceProvider.GetRequiredService<IAgentMemoryRepository>();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<IAgentSessionRepository>();

        _logger.LogInformation("Running memory cleanup...");

        await memoryRepository.CleanupExpiredAsync(stoppingToken);
        _logger.LogInformation("Expired memories cleaned up");

        var adjustedCount = await memoryRepository.AdjustImportanceByUsageAsync(stoppingToken);
        if (adjustedCount > 0)
        {
            _logger.LogInformation("Adjusted importance for {Count} memories based on usage patterns", adjustedCount);
        }

        var cleanedCount = await memoryRepository.CleanupLowValueMemoriesAsync(stoppingToken);
        if (cleanedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} low-value memories (old, unused, low importance)", cleanedCount);
        }

        await sessionRepository.ArchiveStaleSessionsAsync(StaleSessionDays, stoppingToken);
        _logger.LogInformation("Stale sessions archived");
    }
}
