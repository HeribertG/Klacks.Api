// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reacts to a committed region-setup import that actually changed scheduling rule rate revisions or
/// surcharge-relevant rule preset fields, and queues a thorough recalculation. The window starts at
/// the earliest changed revision ValidFrom (falling back to the earliest referencing contract's
/// ValidFrom when only preset fields changed) and ends at the latest existing work date of the
/// affected clients (no hardcoded planning horizon). Skips when no contract references the rules or
/// no works exist in the window. The queued request is scoped to the clients of the referencing
/// contracts, so unrelated clients in the window stay untouched; sealed works are excluded from
/// overwriting by the recalculation pipeline itself. Runs post-commit and fully defensive: a
/// failure is logged and never affects the committed import.
/// </summary>
/// <param name="scope">Resolves referencing contracts, their clients and the latest work date</param>
/// <param name="queue">Thorough recalculation queue fed with the affected window</param>
/// <param name="logger">Logs queued windows and failures</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class ThoroughRecalculationOnRateRevisionsImportedHandler : IDomainEventHandler<SchedulingRuleRateRevisionsImportedEvent>
{
    private readonly ISurchargeRecalculationScope _scope;
    private readonly IThoroughRecalculationQueue _queue;
    private readonly ILogger<ThoroughRecalculationOnRateRevisionsImportedHandler> _logger;

    public ThoroughRecalculationOnRateRevisionsImportedHandler(
        ISurchargeRecalculationScope scope,
        IThoroughRecalculationQueue queue,
        ILogger<ThoroughRecalculationOnRateRevisionsImportedHandler> logger)
    {
        _scope = scope;
        _queue = queue;
        _logger = logger;
    }

    public async Task HandleAsync(SchedulingRuleRateRevisionsImportedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            if (domainEvent.RuleIds.Count == 0)
            {
                return;
            }

            var windows = await _scope.GetContractWindowsForRulesAsync(domainEvent.RuleIds, cancellationToken);
            if (windows.Count == 0)
            {
                return;
            }

            var clientIds = await _scope.GetClientIdsForContractsAsync(
                windows.Select(w => w.ContractId).Distinct().ToList(), cancellationToken);
            if (clientIds.Count == 0)
            {
                return;
            }

            var from = domainEvent.EarliestChangedValidFrom ?? windows.Min(w => w.ValidFrom);
            var until = await _scope.GetLatestWorkDateAsync(clientIds, from, cancellationToken);
            if (until is null)
            {
                return;
            }

            _queue.QueueRecalculation(from, until.Value, null, null, clientIds);
            _logger.LogInformation(
                "Rate revision import changed {RuleCount} scheduling rule(s): queued thorough recalculation for {From}..{Until} ({ClientCount} affected client(s))",
                domainEvent.RuleIds.Count,
                from,
                until,
                clientIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue thorough recalculation after rate revision import for {RuleCount} rule(s); the import is committed and remains unaffected",
                domainEvent.RuleIds.Count);
        }
    }
}
