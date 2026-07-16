// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reacts to a committed scheduling rule change by queueing a thorough recalculation over the
/// validity windows of all contracts that reference the rule. The window starts at the earliest
/// referencing contract's ValidFrom and ends at the latest existing work date of the affected
/// clients (no hardcoded planning horizon). Skips when no contract references the rule or no works
/// exist in the window. The queued request is scoped to the clients of the referencing contracts,
/// so unrelated clients in the window stay untouched; sealed works are excluded from overwriting by
/// the recalculation pipeline itself. Runs post-commit and fully defensive: a failure is logged and
/// never affects the committed rule change.
/// </summary>
/// <param name="scope">Resolves referencing contracts, their clients and the latest work date</param>
/// <param name="queue">Thorough recalculation queue fed with the affected window</param>
/// <param name="logger">Logs queued windows and failures</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class ThoroughRecalculationOnSchedulingRuleChangedHandler : IDomainEventHandler<SchedulingRuleChangedEvent>
{
    private readonly ISurchargeRecalculationScope _scope;
    private readonly IThoroughRecalculationQueue _queue;
    private readonly ILogger<ThoroughRecalculationOnSchedulingRuleChangedHandler> _logger;

    public ThoroughRecalculationOnSchedulingRuleChangedHandler(
        ISurchargeRecalculationScope scope,
        IThoroughRecalculationQueue queue,
        ILogger<ThoroughRecalculationOnSchedulingRuleChangedHandler> logger)
    {
        _scope = scope;
        _queue = queue;
        _logger = logger;
    }

    public async Task HandleAsync(SchedulingRuleChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var windows = await _scope.GetContractWindowsForRulesAsync([domainEvent.RuleId], cancellationToken);
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

            var from = windows.Min(w => w.ValidFrom);
            var until = await _scope.GetLatestWorkDateAsync(clientIds, from, cancellationToken);
            if (until is null)
            {
                return;
            }

            _queue.QueueRecalculation(from, until.Value, null, null, clientIds);
            _logger.LogInformation(
                "Scheduling rule {RuleId} changed: queued thorough recalculation for {From}..{Until} ({ContractCount} referencing contract(s), {ClientCount} affected client(s))",
                domainEvent.RuleId,
                from,
                until,
                windows.Count,
                clientIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue thorough recalculation for changed scheduling rule {RuleId}; the rule change is committed and remains unaffected",
                domainEvent.RuleId);
        }
    }
}
