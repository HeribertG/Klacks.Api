// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetUnresolvedEscalationChainsQuery : IRequest<IReadOnlyList<UnresolvedEscalationChainResource>>
{
    public GetUnresolvedEscalationChainsQuery(string currentUserId)
    {
        CurrentUserId = currentUserId;
    }

    public string CurrentUserId { get; }
}
