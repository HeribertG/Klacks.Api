// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Timer host of the learning loop: every six hours it does the housekeeping - thresholds, retention,
/// the weekly usefulness snapshots and the pruning of what did not earn its place - and then runs one
/// learning pass. Replaces SkillGapSuggestionBackgroundService, which asked a language model
/// for a skill name on every run and pushed the raw user text to every connected client; nothing here
/// reaches a user at all. The weekly digest is NOT driven from here - it rides the existing hourly
/// detector tick, so the pipeline keeps exactly one clock for proactive events.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider for the housekeeping sweep</param>
/// <param name="launcher">Owns the gate that keeps this tick and a manual trigger from overlapping</param>
/// <param name="logger">Structured log per run</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class SkillLearningBackgroundService : BackgroundService
{
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(6);
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISkillLearningRunLauncher _launcher;
    private readonly ILogger<SkillLearningBackgroundService> _logger;

    public SkillLearningBackgroundService(
        IServiceScopeFactory scopeFactory,
        ISkillLearningRunLauncher launcher,
        ILogger<SkillLearningBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _launcher = launcher;
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
                    _logger.LogError(exception, "Skill learning run failed");
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
        using (var scope = _scopeFactory.CreateScope())
        {
            var maintenance = scope.ServiceProvider.GetRequiredService<ISkillLearningMaintenanceService>();
            await maintenance.RunAsync(cancellationToken);
        }

        var ticket = await _launcher.RunAsync(cancellationToken);
        if (!ticket.Started)
        {
            _logger.LogInformation("Skill learning tick skipped: {Reason}", ticket.Reason);
        }
    }
}
