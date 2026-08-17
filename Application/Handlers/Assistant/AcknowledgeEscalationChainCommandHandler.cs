// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The intervention list's "übernehmen" action.
/// </summary>
/// <param name="chainService">Owns the conditional stage/chain transition and the handoff notification.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class AcknowledgeEscalationChainCommandHandler : IRequestHandler<AcknowledgeEscalationChainCommand, HttpResultResource>
{
    private const string SuccessMessage = "Escalation chain acknowledged.";
    private const string FailureMessage = "You do not currently hold a notified stage on this chain.";

    private readonly IEscalationChainService _chainService;

    public AcknowledgeEscalationChainCommandHandler(IEscalationChainService chainService)
    {
        _chainService = chainService;
    }

    public async Task<HttpResultResource> Handle(AcknowledgeEscalationChainCommand request, CancellationToken cancellationToken)
    {
        var success = await _chainService.AcknowledgeChainAsync(request.ChainId, request.UserId, cancellationToken);
        return new HttpResultResource { Success = success, Messages = success ? SuccessMessage : FailureMessage };
    }
}
