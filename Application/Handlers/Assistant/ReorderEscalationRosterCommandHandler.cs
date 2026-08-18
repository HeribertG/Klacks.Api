// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies an admin's drag'n'drop reorder of the escalation roster.
/// </summary>
/// <param name="rosterService">Owns the AppUser.EscalationRosterOrder write.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ReorderEscalationRosterCommandHandler : IRequestHandler<ReorderEscalationRosterCommand, HttpResultResource>
{
    private readonly IEscalationRosterService _rosterService;

    public ReorderEscalationRosterCommandHandler(IEscalationRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    public async Task<HttpResultResource> Handle(ReorderEscalationRosterCommand request, CancellationToken cancellationToken)
    {
        return await _rosterService.ReorderAsync(request.OrderedUserIds, cancellationToken);
    }
}
