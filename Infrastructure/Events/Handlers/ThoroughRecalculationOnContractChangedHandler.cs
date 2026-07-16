// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reacts to a committed contract change by queueing a thorough recalculation of the affected date
/// window, so already-persisted work surcharges pick up retroactive rate changes. The window end is
/// the event's explicit end or, when absent, the latest existing work date of the affected clients
/// (no hardcoded planning horizon). Skips entirely when no works exist in the window. The queued
/// request is scoped to the affected clients, so unrelated clients in the window stay untouched;
/// sealed works are excluded from overwriting by the recalculation pipeline itself. Runs
/// post-commit and fully defensive: a failure is logged and never affects the committed contract
/// change.
/// </summary>
/// <param name="scope">Resolves affected clients and the latest work date</param>
/// <param name="queue">Thorough recalculation queue fed with the affected window</param>
/// <param name="logger">Logs queued windows and failures</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class ThoroughRecalculationOnContractChangedHandler : IDomainEventHandler<ContractChangedEvent>
{
    private readonly ISurchargeRecalculationScope _scope;
    private readonly IThoroughRecalculationQueue _queue;
    private readonly ILogger<ThoroughRecalculationOnContractChangedHandler> _logger;

    public ThoroughRecalculationOnContractChangedHandler(
        ISurchargeRecalculationScope scope,
        IThoroughRecalculationQueue queue,
        ILogger<ThoroughRecalculationOnContractChangedHandler> logger)
    {
        _scope = scope;
        _queue = queue;
        _logger = logger;
    }

    public async Task HandleAsync(ContractChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var clientIds = domainEvent.ClientId.HasValue
                ? new List<Guid> { domainEvent.ClientId.Value }
                : await _scope.GetClientIdsForContractAsync(domainEvent.ContractId, cancellationToken);

            if (clientIds.Count == 0)
            {
                return;
            }

            var from = domainEvent.RecalculationFrom;
            var until = domainEvent.RecalculationUntil
                ?? await _scope.GetLatestWorkDateAsync(clientIds, from, cancellationToken);

            if (until is null || until < from)
            {
                return;
            }

            if (!await _scope.HasWorksInWindowAsync(clientIds, from, until.Value, cancellationToken))
            {
                return;
            }

            _queue.QueueRecalculation(from, until.Value, null, null, clientIds);
            _logger.LogInformation(
                "Contract {ContractId} changed: queued thorough recalculation for {From}..{Until} ({ClientCount} affected client(s))",
                domainEvent.ContractId,
                from,
                until,
                clientIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue thorough recalculation for changed contract {ContractId}; the contract change is committed and remains unaffected",
                domainEvent.ContractId);
        }
    }
}
