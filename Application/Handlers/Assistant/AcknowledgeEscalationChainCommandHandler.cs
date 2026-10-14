// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The intervention list's "übernehmen" action. Returns the outcome unchanged instead of a boolean so the
/// controller can answer a too-late acknowledgement with 409 rather than a 200 that pretends something was
/// released; the chain's status is read back rather than derived from the outcome, because only the row
/// itself knows whether the chain was exhausted, cancelled or superseded in the meantime. A chain the caller may not
/// see is answered exactly like a missing one (NoNotifiedStage, no status) before the service is reached.
/// </summary>
/// <param name="chainService">Owns the conditional stage/chain transition and the handoff notification.</param>
/// <param name="chainRepository">Read-only here: supplies the chain and its status for the response.</param>
/// <param name="visibilityFilter">Hides chains the caller may not see.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class AcknowledgeEscalationChainCommandHandler
    : IRequestHandler<AcknowledgeEscalationChainCommand, EscalationAcknowledgeResultResource>
{
    private readonly IEscalationChainService _chainService;
    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationChainVisibilityFilter _visibilityFilter;

    public AcknowledgeEscalationChainCommandHandler(
        IEscalationChainService chainService,
        IEscalationChainRepository chainRepository,
        IEscalationChainVisibilityFilter visibilityFilter)
    {
        _chainService = chainService;
        _chainRepository = chainRepository;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<EscalationAcknowledgeResultResource> Handle(
        AcknowledgeEscalationChainCommand request, CancellationToken cancellationToken)
    {
        var chain = await _chainRepository.GetByIdWithStagesAsync(request.ChainId, cancellationToken);
        if (chain is not null && !await _visibilityFilter.IsVisibleAsync(chain, request.UserId, cancellationToken))
        {
            return new EscalationAcknowledgeResultResource
            {
                Outcome = EscalationAcknowledgeOutcome.NoNotifiedStage,
                ChainStatus = null
            };
        }

        var outcome = await _chainService.AcknowledgeChainAsync(request.ChainId, request.UserId, cancellationToken);
        var chainStatus = await _chainRepository.GetStatusAsync(request.ChainId, cancellationToken);

        return new EscalationAcknowledgeResultResource
        {
            Outcome = outcome,
            ChainStatus = chainStatus
        };
    }
}
