// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Timer host for the learning loop's housekeeping. Replaces SkillGapSuggestionBackgroundService, which
/// asked a language model for a skill name on every run and pushed the raw user text to every connected
/// client; this one only sweeps thresholds and retention and talks to nobody. The weekly digest is NOT
/// driven from here - it rides the existing hourly detector tick, so the pipeline keeps exactly one clock
/// for proactive events.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per run</param>
/// <param name="logger">Structured log per run</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class SkillLearningBackgroundService : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillLearningBackgroundService> _logger;

    public SkillLearningBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SkillLearningBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Skill learning background service started; runs every {Hours}h", RunInterval.TotalHours);

        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(RunInterval);
        try
        {
            do
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Skill learning maintenance run failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Skill learning background service stopped");
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var maintenance = scope.ServiceProvider.GetRequiredService<ISkillLearningMaintenanceService>();
        await maintenance.RunAsync(cancellationToken);
    }
}
