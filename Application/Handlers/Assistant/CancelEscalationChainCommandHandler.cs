// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The intervention list's "abbrechen" action (Owner decision B7). A chain the caller may not see is answered exactly like
/// a chain that is already resolved or missing; a visible chain may only be cancelled by an administrator, a supervisor
/// or a roster member holding an open stage of it, anybody else gets the Forbidden outcome (the controller answers 403).
/// </summary>
/// <param name="chainService">Owns the conditional cancel transition and cancels the remaining stages.</param>
/// <param name="chainRepository">Loads the chain so its visibility and the caller's stage can be checked.</param>
/// <param name="visibilityFilter">Hides chains the caller may not see.</param>
/// <param name="userService">Tells whether the caller holds the Admin or Authorised role.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CancelEscalationChainCommandHandler : IRequestHandler<CancelEscalationChainCommand, EscalationCancelResultResource>
{
    private const string SuccessMessage = "Escalation chain cancelled.";
    private const string FailureMessage = "This chain was already resolved.";
    private const string ForbiddenMessage = "Only an administrator, a supervisor or a member of this chain's roster may cancel it.";

    private readonly IEscalationChainService _chainService;
    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationChainVisibilityFilter _visibilityFilter;
    private readonly IUserService _userService;

    public CancelEscalationChainCommandHandler(
        IEscalationChainService chainService,
        IEscalationChainRepository chainRepository,
        IEscalationChainVisibilityFilter visibilityFilter,
        IUserService userService)
    {
        _chainService = chainService;
        _chainRepository = chainRepository;
        _visibilityFilter = visibilityFilter;
        _userService = userService;
    }

    public async Task<EscalationCancelResultResource> Handle(CancelEscalationChainCommand request, CancellationToken cancellationToken)
    {
        var chain = await _chainRepository.GetByIdWithStagesAsync(request.ChainId, cancellationToken);
        if (chain is not null)
        {
            if (!await _visibilityFilter.IsVisibleAsync(chain, request.UserId, cancellationToken))
            {
                return Result(EscalationCancelOutcome.AlreadyResolved, success: false, FailureMessage);
            }

            var isAdmin = await _userService.IsAdmin();
            var isSupervisor = _userService.GetRights().Contains(Roles.Authorised);
            if (!EscalationChainCancelPolicy.MayCancel(chain, request.UserId, isAdmin, isSupervisor))
            {
                return Result(EscalationCancelOutcome.Forbidden, success: false, ForbiddenMessage);
            }
        }

        var success = await _chainService.CancelAsync(
            request.ChainId, request.UserId, request.UserName, request.Reason, cancellationToken);
        return success
            ? Result(EscalationCancelOutcome.Cancelled, success: true, SuccessMessage)
            : Result(EscalationCancelOutcome.AlreadyResolved, success: false, FailureMessage);
    }

    private static EscalationCancelResultResource Result(EscalationCancelOutcome outcome, bool success, string message)
    {
        return new EscalationCancelResultResource(outcome, new HttpResultResource { Success = success, Messages = message });
    }
}
