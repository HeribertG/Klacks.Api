// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reacts to a committed change of surcharge-relevant global settings by queueing a thorough
/// recalculation. Global settings carry no validity date, so the window spans from the earliest to
/// the latest real-mode work date with LockLevel None (sealed works are additionally protected by
/// the recalculation pipeline's own sealed skip). Skips entirely when no such work exists. Settings
/// affect every client, so the queued request carries no client scope. Runs post-commit and fully
/// defensive: a failure is logged and never affects the committed settings change.
/// </summary>
/// <param name="scope">Resolves the unlocked real-mode work window</param>
/// <param name="queue">Thorough recalculation queue fed with the affected window</param>
/// <param name="logger">Logs queued windows and failures</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class ThoroughRecalculationOnSurchargeSettingsChangedHandler : IDomainEventHandler<SurchargeSettingsChangedEvent>
{
    private readonly ISurchargeRecalculationScope _scope;
    private readonly IThoroughRecalculationQueue _queue;
    private readonly ILogger<ThoroughRecalculationOnSurchargeSettingsChangedHandler> _logger;

    public ThoroughRecalculationOnSurchargeSettingsChangedHandler(
        ISurchargeRecalculationScope scope,
        IThoroughRecalculationQueue queue,
        ILogger<ThoroughRecalculationOnSurchargeSettingsChangedHandler> logger)
    {
        _scope = scope;
        _queue = queue;
        _logger = logger;
    }

    public async Task HandleAsync(SurchargeSettingsChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var window = await _scope.GetUnlockedRealWorkWindowAsync(cancellationToken);
            if (window is null)
            {
                return;
            }

            _queue.QueueRecalculation(window.From, window.Until, null, null);
            _logger.LogInformation(
                "Surcharge-relevant setting(s) changed ({Keys}): queued thorough recalculation for {From}..{Until} (all clients)",
                string.Join(", ", domainEvent.ChangedKeys),
                window.From,
                window.Until);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue thorough recalculation after surcharge-relevant settings change ({Keys}); the settings change is committed and remains unaffected",
                string.Join(", ", domainEvent.ChangedKeys));
        }
    }
}
