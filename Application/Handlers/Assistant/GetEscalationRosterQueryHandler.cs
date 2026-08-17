// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists a group's visible members for the admin roster card, unfiltered by absence or phone number.
/// </summary>
/// <param name="rosterService">Resolves the group's visible members.</param>

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
        var members = await _rosterService.GetRosterMembersAsync(request.GroupId, cancellationToken);

        return members.Select(member => new EscalationRosterMemberResource
        {
            UserId = member.UserId,
            DisplayName = member.DisplayName,
            HasPhoneNumber = member.HasPhoneNumber,
            IsCurrentlyAbsent = member.IsCurrentlyAbsent
        }).ToList();
    }
}
