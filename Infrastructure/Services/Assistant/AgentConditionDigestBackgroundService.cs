// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Owns the daily digest's own, independent clock - deliberately not the 60-minute tick
/// AgentTriggerBackgroundService already runs. Polls IAgentConditionDigestService.RunIfDueAsync every
/// PollInterval; the service itself decides whether the installation's configured local time of day has
/// been reached and whether today already ran, so this host only supplies the timer, a scoped DI
/// provider per check and cooperative cancellation - the same division of responsibility as
/// ScheduledTaskBackgroundService and GoalReflectionBackgroundService. Checking immediately after the
/// startup delay (rather than only on the first PollInterval tick) is what gives a server that restarted
/// after the target time its catch-up run without waiting up to PollInterval longer than necessary.
/// </summary>
/// <param name="scopeFactory">Creates a scoped DI provider per check.</param>
/// <param name="logger">Structured log per check.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class AgentConditionDigestBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AgentConditionDigestBackgroundService> _logger;

    public AgentConditionDigestBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AgentConditionDigestBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Agent condition digest background service started; checks every {Minutes}min, default target {Default} local",
            PollInterval.TotalMinutes, AgentConditionDigestDefaults.DefaultTimeOfDayLocal);

        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            do
            {
                try
                {
                    await CheckOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Agent condition digest check failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Agent condition digest background service stopped");
    }

    private async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var digestService = scope.ServiceProvider.GetRequiredService<IAgentConditionDigestService>();
        var result = await digestService.RunIfDueAsync(cancellationToken);

        if (result.Outcome == AgentConditionDigestOutcome.Ran)
        {
            _logger.LogInformation("Agent condition digest sent to {Count} planner(s)", result.RecipientsNotified);
        }
        else
        {
            _logger.LogDebug("Agent condition digest check: {Outcome}", result.Outcome);
        }
    }
}
