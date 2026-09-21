// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The intervention list's "übernehmen" action. Returns the outcome unchanged instead of a boolean so the
/// controller can answer a too-late acknowledgement with 409 rather than a 200 that pretends something was
/// released; the chain's status is read back rather than derived from the outcome, because only the row
/// itself knows whether the chain was exhausted, cancelled or superseded in the meantime.
/// </summary>
/// <param name="chainService">Owns the conditional stage/chain transition and the handoff notification.</param>
/// <param name="chainRepository">Read-only here: supplies the chain's status for the response.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class AcknowledgeEscalationChainCommandHandler
    : IRequestHandler<AcknowledgeEscalationChainCommand, EscalationAcknowledgeResultResource>
{
    private readonly IEscalationChainService _chainService;
    private readonly IEscalationChainRepository _chainRepository;

    public AcknowledgeEscalationChainCommandHandler(
        IEscalationChainService chainService, IEscalationChainRepository chainRepository)
    {
        _chainService = chainService;
        _chainRepository = chainRepository;
    }

    public async Task<EscalationAcknowledgeResultResource> Handle(
        AcknowledgeEscalationChainCommand request, CancellationToken cancellationToken)
    {
        var outcome = await _chainService.AcknowledgeChainAsync(request.ChainId, request.UserId, cancellationToken);
        var chainStatus = await _chainRepository.GetStatusAsync(request.ChainId, cancellationToken);

        return new EscalationAcknowledgeResultResource
        {
            Outcome = outcome,
            ChainStatus = chainStatus
        };
    }
}
