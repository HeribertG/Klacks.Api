// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Starts learning runs and keeps them from overlapping. Inside this process a single gate does that
/// outright: the six-hourly tick and an administrator's manual trigger cannot run at the same time, and
/// the manual trigger is told so instead of silently queueing behind one. Across instances the guarantee
/// comes from the per-cluster compare-and-swap claim in the loop, not from here - two runs on two
/// machines would claim disjoint clusters, so no cluster is ever learned twice.
/// The manual path starts the run in the background: a run rebuilds the knowledge index several times and
/// takes minutes, which no HTTP request may wait for.
/// </summary>
/// <param name="scopeFactory">Creates the scoped provider a run needs, independent of the caller's scope</param>
/// <param name="logger">Reports runs that failed outright</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public sealed class SkillLearningRunLauncher : ISkillLearningRunLauncher
{
    private const string AlreadyRunning = "A learning run is already in progress.";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillLearningRunLauncher> _logger;

    public SkillLearningRunLauncher(
        IServiceScopeFactory scopeFactory,
        ILogger<SkillLearningRunLauncher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<SkillLearningRunTicket> RunAsync(CancellationToken cancellationToken = default)
    {
        if (!_gate.Wait(0, cancellationToken))
        {
            return SkillLearningRunTicket.Refused(AlreadyRunning);
        }

        await ExecuteAsync(cancellationToken);
        return SkillLearningRunTicket.Accepted();
    }

    public SkillLearningRunTicket StartDetached()
    {
        if (!_gate.Wait(0))
        {
            return SkillLearningRunTicket.Refused(AlreadyRunning);
        }

        _ = Task.Run(() => ExecuteAsync(CancellationToken.None));
        return SkillLearningRunTicket.Accepted();
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var loop = scope.ServiceProvider.GetRequiredService<ISkillLearningLoop>();
            await loop.RunAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Skill learning run failed");
        }
        finally
        {
            _gate.Release();
        }
    }
}
