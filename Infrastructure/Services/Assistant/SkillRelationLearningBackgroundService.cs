// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Periodically runs the skill-relation learner in its own scope, off the hot path. Failures of an
/// iteration are logged and swallowed so the host is never affected. Phase 2 only computes/updates
/// edges; it does not change skill-selection behaviour.
/// </summary>
/// <param name="serviceProvider">Used to create a scope per iteration for the scoped learner.</param>
/// <param name="logger">Diagnostic logging of iteration failures.</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class SkillRelationLearningBackgroundService : BackgroundService
{
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkillRelationLearningBackgroundService> _logger;

    public SkillRelationLearningBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SkillRelationLearningBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var learner = scope.ServiceProvider.GetRequiredService<ISkillRelationLearner>();
                await learner.LearnAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skill-relation learning iteration failed.");
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
