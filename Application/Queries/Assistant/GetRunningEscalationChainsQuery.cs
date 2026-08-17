// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetRunningEscalationChainsQuery : IRequest<IReadOnlyList<EscalationChainSummaryResource>>
{
    public GetRunningEscalationChainsQuery(string currentUserId)
    {
        CurrentUserId = currentUserId;
    }

    public string CurrentUserId { get; }
}
