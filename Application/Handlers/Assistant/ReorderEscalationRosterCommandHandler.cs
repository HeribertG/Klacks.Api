// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies an admin's manual reorder of a group's escalation call list.
/// </summary>
/// <param name="rosterService">Owns the OverrideRank write and the re-derivation it runs first.</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ReorderEscalationRosterCommandHandler : IRequestHandler<ReorderEscalationRosterCommand, HttpResultResource>
{
    private const string SuccessMessage = "Roster order updated.";

    private readonly IEscalationRosterService _rosterService;

    public ReorderEscalationRosterCommandHandler(IEscalationRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    public async Task<HttpResultResource> Handle(ReorderEscalationRosterCommand request, CancellationToken cancellationToken)
    {
        await _rosterService.SetOrderAsync(request.GroupId, request.OrderedUserIds, cancellationToken);
        return new HttpResultResource { Success = true, Messages = SuccessMessage };
    }
}
