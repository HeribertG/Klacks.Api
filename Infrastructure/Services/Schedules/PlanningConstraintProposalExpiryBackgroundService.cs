// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Background sweep that lets undecided planning-constraint proposals expire: every Proposed row older than
/// PlanningConstraintDefaults.ProposalLifetimeDays is moved to Rejected (the model has no separate Expired
/// state; CurrentUserUpdated carries PlanningConstraintDefaults.ExpirySweepActor). One conditional bulk update
/// per cycle, so a second instance racing the same tick changes nothing twice. Approval of an already
/// expired proposal is refused by the approve handler independently of this sweep.
/// </summary>
/// <param name="scopeFactory">Creates the DI scope for each sweep</param>
/// <param name="timeProvider">Clock for the expiry cut-off</param>
/// <param name="logger">Logger instance</param>

using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class PlanningConstraintProposalExpiryBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlanningConstraintProposalExpiryBackgroundService> _logger;

    public PlanningConstraintProposalExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        ILogger<PlanningConstraintProposalExpiryBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> SweepOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IPlanningConstraintRepository>();
        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var expired = await repository.ExpireProposedAsync(nowUtc - PlanningConstraintLifecycle.ProposalLifetime, nowUtc, cancellationToken);
        if (expired > 0)
        {
            _logger.LogInformation("Expired {Count} undecided planning-constraint proposal(s)", expired);
        }

        return expired;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(InitialDelay, _timeProvider, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SweepOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in planning-constraint proposal expiry sweep");
                }

                await Task.Delay(Interval, _timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
