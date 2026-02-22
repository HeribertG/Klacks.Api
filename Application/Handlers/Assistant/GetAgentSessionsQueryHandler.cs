// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAgentSessionsQueryHandler : IRequestHandler<GetAgentSessionsQuery, object>
{
    private readonly IAgentSessionRepository _sessionRepository;

    public GetAgentSessionsQueryHandler(IAgentSessionRepository sessionRepository)
    {
        _sessionRepository = sessionRepository;
    }

    public async Task<object> Handle(GetAgentSessionsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _sessionRepository.GetUserSessionsAsync(request.UserId, 50, cancellationToken);
        return sessions.Select(s => new
        {
            s.Id, s.SessionId, s.Title, s.Status,
            s.MessageCount, s.TokenCountEst, s.LastMessageAt, s.IsArchived
        }).ToList();
    }
}
