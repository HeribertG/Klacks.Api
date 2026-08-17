// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetEscalationRosterQuery : IRequest<IReadOnlyList<EscalationRosterMemberResource>>
{
    public GetEscalationRosterQuery(Guid groupId)
    {
        GroupId = groupId;
    }

    public Guid GroupId { get; }
}
