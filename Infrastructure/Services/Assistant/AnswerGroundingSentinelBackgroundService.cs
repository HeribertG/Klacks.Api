// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that periodically runs the answer grounding sentinel probe so a dead
/// evaluator becomes visible as a missing sentinel finding instead of masquerading as an honest
/// assistant. Runs shortly after startup so every deployment proves the pipeline once.
/// </summary>
/// <param name="scopeFactory">Factory for creating DI scopes in background tasks</param>
/// <param name="options">Bound Assistant:AnswerGroundingMode configuration (Off keeps the service idle)</param>
/// <param name="logger">Logger instance</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Grounding;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class AnswerGroundingSentinelBackgroundService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly AnswerGroundingOptions _options;
    private readonly ILogger<AnswerGroundingSentinelBackgroundService> _logger;

    public AnswerGroundingSentinelBackgroundService(
        IServiceScopeFactory scopeFactory,
        AnswerGroundingOptions options,
        ILogger<AnswerGroundingSentinelBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.IsEnabled)
        {
            return;
        }

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var probe = scope.ServiceProvider.GetRequiredService<IAnswerGroundingSentinelProbe>();
                    await probe.RunAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in answer grounding sentinel background service");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
