// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.ScheduleOptimizer.TokenEvolution;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Executes wizard GA jobs on the thread-pool and streams progress via SignalR.
/// Each job runs in its own DI scope so scoped repositories/DbContext are properly disposed.
/// </summary>
public sealed class WizardJobRunner : IWizardJobRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<WizardJobHub, IWizardJobClient> _hubContext;
    private readonly WizardJobRegistry _registry;
    private readonly WizardResultCache _resultCache;
    private readonly ILogger<WizardJobRunner> _logger;

    public WizardJobRunner(
        IServiceScopeFactory scopeFactory,
        IHubContext<WizardJobHub, IWizardJobClient> hubContext,
        WizardJobRegistry registry,
        WizardResultCache resultCache,
        ILogger<WizardJobRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _registry = registry;
        _resultCache = resultCache;
        _logger = logger;
    }

    public Task<Guid> StartAsync(WizardContextRequest request, CancellationToken externalCt)
    {
        var jobId = Guid.NewGuid();
        var cts = _registry.Register(jobId, externalCt);

        _ = Task.Run(() => RunJobAsync(jobId, request, cts.Token));

        return Task.FromResult(jobId);
    }

    public bool TryCancel(Guid jobId) => _registry.TryCancel(jobId);

    public bool IsRunning(Guid jobId) => _registry.IsRunning(jobId);

    private async Task RunJobAsync(Guid jobId, WizardContextRequest request, CancellationToken ct)
    {
        var group = _hubContext.Clients.Group(SignalRConstants.WizardGroups.WizardJob(jobId));

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var builder = scope.ServiceProvider.GetRequiredService<IWizardContextBuilder>();
            var wizardContext = await builder.BuildContextAsync(request, ct);

            var config = new TokenEvolutionConfig
            {
                RandomSeed = Guid.NewGuid().GetHashCode(),
            };

            var loop = TokenEvolutionLoop.Create();

            var progress = new Progress<TokenEvolutionProgress>(p =>
            {
                try
                {
                    _ = group.OnProgress(new WizardJobProgressDto(
                        JobId: jobId,
                        Generation: p.Generation,
                        MaxGenerations: p.MaxGenerations,
                        BestHardViolations: p.BestHardViolations,
                        BestStage1Completion: p.BestStage1Completion,
                        BestStage2Score: p.BestStage2Score,
                        EarlyStopping: p.EarlyStopping));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to broadcast wizard progress for job {JobId}", jobId);
                }
            });

            var best = loop.Run(wizardContext, config, progress, ct);
            _resultCache.Store(jobId, best);

            await group.OnCompleted(new WizardJobResultDto(
                JobId: jobId,
                FinalHardViolations: best.FitnessStage0,
                FinalStage1Completion: best.FitnessStage1,
                TokenCount: best.Tokens.Count));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Wizard job {JobId} cancelled", jobId);
            try
            {
                await group.OnCancelled();
            }
            catch
            {
                // Swallow notification errors during cancellation.
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Wizard job {JobId} failed", jobId);
            try
            {
                await group.OnFailed(ex.Message);
            }
            catch
            {
                // Swallow notification errors during failure.
            }
        }
        finally
        {
            _registry.Remove(jobId);
        }
    }
}
