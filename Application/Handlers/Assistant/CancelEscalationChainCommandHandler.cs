// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The intervention list's "abbrechen" action (Owner decision B7).
/// </summary>
/// <param name="chainService">Owns the conditional cancel transition and cancels the remaining stages.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CancelEscalationChainCommandHandler : IRequestHandler<CancelEscalationChainCommand, HttpResultResource>
{
    private const string SuccessMessage = "Escalation chain cancelled.";
    private const string FailureMessage = "This chain was already resolved.";

    private readonly IEscalationChainService _chainService;

    public CancelEscalationChainCommandHandler(IEscalationChainService chainService)
    {
        _chainService = chainService;
    }

    public async Task<HttpResultResource> Handle(CancelEscalationChainCommand request, CancellationToken cancellationToken)
    {
        var success = await _chainService.CancelAsync(
            request.ChainId, request.UserId, request.UserName, request.Reason, cancellationToken);
        return new HttpResultResource { Success = success, Messages = success ? SuccessMessage : FailureMessage };
    }
}
