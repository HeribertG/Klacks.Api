// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists every user with any GroupVisibility and a phone number, for the admin roster card.
/// </summary>
/// <param name="rosterService">Resolves the flat, group-agnostic member list.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetEscalationRosterQueryHandler : IRequestHandler<GetEscalationRosterQuery, IReadOnlyList<EscalationRosterMemberResource>>
{
    private readonly IEscalationRosterService _rosterService;

    public GetEscalationRosterQueryHandler(IEscalationRosterService rosterService)
    {
        _rosterService = rosterService;
    }

    public async Task<IReadOnlyList<EscalationRosterMemberResource>> Handle(GetEscalationRosterQuery request, CancellationToken cancellationToken)
    {
        var members = await _rosterService.GetRosterMembersAsync(cancellationToken);

        return members.Select(member => new EscalationRosterMemberResource
        {
            UserId = member.UserId,
            DisplayName = member.DisplayName,
            IsCurrentlyAbsent = member.IsCurrentlyAbsent
        }).ToList();
    }
}
